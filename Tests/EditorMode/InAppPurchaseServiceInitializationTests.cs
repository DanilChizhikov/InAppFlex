using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class InAppPurchaseServiceInitializationTests
    {
        private ServiceFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ServiceFixture();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void InitializeConnectsThenFetchesProductsAndPurchases()
        {
            _fixture.Service.Initialize();

            Assert.IsTrue(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.InitializedCount);
            Assert.AreEqual(1, _fixture.Store.ConnectCalls);
            Assert.AreEqual(1, _fixture.Products.FetchCalls);
            CollectionAssert.AreEquivalent(
                new[] { ServiceFixture.CoinsStoreId, ServiceFixture.VipStoreId, ServiceFixture.NoAdsStoreId },
                _fixture.Products.LastRequested.Select(definition => definition.id));
            Assert.AreEqual(1, _fixture.Purchases.FetchCalls);
            Assert.AreEqual(false, _fixture.Purchases.ProcessPendingOrdersOnFetch);
        }

        [Test]
        public void InitializeFailsWhenStoreReturnsNoProducts()
        {
            _fixture.Products.RemoveAllProducts();

            LogAssert.Expect(ServiceFixture.StoreFailureLogType, new Regex("Fetch products failed: products not found"));
            _fixture.Service.Initialize();

            Assert.IsFalse(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.InitializeFailures.Count);
            Assert.AreEqual(0, _fixture.Purchases.FetchCalls);
        }

        [Test]
        public void InitializeWarnsAboutProductsWithoutMetadata()
        {
            _fixture.Products.RemoveProduct(ServiceFixture.NoAdsStoreId);

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Store returned no metadata for: {ServiceFixture.NoAdsStoreId}");
            _fixture.Service.Initialize();

            Assert.IsTrue(_fixture.Service.IsInitialized);
            Assert.IsEmpty(_fixture.InitializeFailures);
        }

        [Test]
        public void InitializeFailsWhenPurchasesFetchFails()
        {
            _fixture.Purchases.FetchedOrders = null;
            _fixture.Service.Initialize();

            LogAssert.Expect(ServiceFixture.StoreFailureLogType, new Regex("Fetch purchases failed: offline"));
            _fixture.Purchases.RaiseFetchFailed("offline");

            Assert.IsFalse(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.InitializeFailures.Count);
        }

        [Test]
        public void DisconnectDuringInitializeRaisesFailure()
        {
            _fixture.Store.ConnectSucceeds = false;

            LogAssert.Expect(LogType.Warning, new Regex("Store disconnected: no store"));
            _fixture.Service.Initialize();

            Assert.IsFalse(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.InitializeFailures.Count);
            Assert.AreEqual(0, _fixture.Products.FetchCalls);
        }

        [Test]
        public void InitializeAfterFailureSubscribesOnce()
        {
            _fixture.Store.ConnectSucceeds = false;
            LogAssert.Expect(LogType.Warning, new Regex("Store disconnected: no store"));
            _fixture.Service.Initialize();

            _fixture.Store.ConnectSucceeds = true;
            _fixture.Service.Initialize();

            Assert.IsTrue(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.Store.ConnectedSubscribers);
            Assert.AreEqual(1, _fixture.Purchases.PendingSubscribers);
        }

        [Test]
        public void DisposeUnsubscribesFromStore()
        {
            _fixture.Service.Initialize();

            _fixture.Service.Dispose();

            Assert.IsFalse(_fixture.Service.IsInitialized);
            Assert.AreEqual(0, _fixture.Store.ConnectedSubscribers);
            Assert.AreEqual(0, _fixture.Store.DisconnectedSubscribers);
            Assert.AreEqual(0, _fixture.Purchases.PendingSubscribers);
        }

        [Test]
        public void PendingOrderFromPreviousSessionIsDeliveredAfterInitialized()
        {
            _fixture.Purchases.FetchedOrders = StoreFixtures.CreateOrders(StoreFixtures.CreatePendingOrder(_fixture.Coins, "t1"));
            var events = new List<string>();
            _fixture.Service.OnInitialized += () => events.Add("initialized");
            _fixture.Service.OnPurchased += response => events.Add($"purchased:{response.ProductId}");

            _fixture.Service.Initialize();

            CollectionAssert.AreEqual(new[] { "initialized", "purchased:coins" }, events);
            IPurchaseResponse restored = _fixture.Purchased[0];
            Assert.IsTrue(restored.IsRestored);
            Assert.IsFalse(restored.IsAutoConfirm);
            Assert.AreEqual(PurchaseStatus.Success, restored.Status);
            Assert.AreEqual("t1", restored.TransactionId);
            Assert.AreSame(_fixture.Coins, restored.Product);
            Assert.IsEmpty(_fixture.Purchases.ConfirmedOrders);
        }

        [Test]
        public void RestoredPendingOrderIsDeliveredOnce()
        {
            _fixture.Purchases.FetchedOrders = StoreFixtures.CreateOrders(StoreFixtures.CreatePendingOrder(_fixture.Coins, "t1"));
            _fixture.Service.Initialize();

            _fixture.Purchases.FetchPurchases();

            Assert.AreEqual(1, _fixture.Purchased.Count);
        }

        [Test]
        public void RestoredPendingOrderIsConfirmedByConsumer()
        {
            PendingOrder order = StoreFixtures.CreatePendingOrder(_fixture.Coins, "t1");
            _fixture.Purchases.FetchedOrders = StoreFixtures.CreateOrders(order);
            _fixture.Service.Initialize();

            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            Assert.AreEqual(1, _fixture.Purchases.ConfirmedOrders.Count);
            Assert.AreSame(order, _fixture.Purchases.ConfirmedOrders[0]);
        }

        [Test]
        public void PendingOrderForUnknownProductIsSkipped()
        {
            Product other = StoreFixtures.CreateProduct("com.game.other", ProductType.Consumable, 1m, "USD");
            _fixture.Purchases.FetchedOrders = StoreFixtures.CreateOrders(StoreFixtures.CreatePendingOrder(other, "t2"));

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Pending order for unconfigured product 'com.game.other' is skipped.");
            _fixture.Service.Initialize();

            Assert.IsTrue(_fixture.Service.IsInitialized);
            Assert.IsEmpty(_fixture.Purchased);
        }
    }
}