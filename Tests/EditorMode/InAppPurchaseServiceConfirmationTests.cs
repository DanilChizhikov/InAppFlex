using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class InAppPurchaseServiceConfirmationTests
    {
        private ServiceFixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ServiceFixture();
            _fixture.Service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _fixture.Dispose();
        }

        [Test]
        public void ConfirmedOrderRaisesPurchaseConfirmed()
        {
            _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);
            int fetchCalls = _fixture.Purchases.FetchCalls;

            _fixture.Purchases.RaiseConfirmed(StoreFixtures.CreateConfirmedOrder(_fixture.Coins, "t1"));

            Assert.AreEqual(1, _fixture.Confirmed.Count);
            Assert.AreEqual(PurchaseStatus.Success, _fixture.Confirmed[0].Status);
            Assert.AreEqual("coins", _fixture.Confirmed[0].ProductId);
            Assert.AreEqual(fetchCalls + 1, _fixture.Purchases.FetchCalls);
        }

        [Test]
        public void FailedConfirmationReportsReason()
        {
            PendingOrder order = _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            LogAssert.Expect(LogType.Warning, new Regex("Confirm coins failed: reason=PurchaseMissing"));
            _fixture.Purchases.RaiseConfirmed(new FailedOrder(order, PurchaseFailureReason.PurchaseMissing, "Purchase not found"));

            Assert.AreEqual(PurchaseStatus.Failure, _fixture.Confirmed[0].Status);
            Assert.AreEqual(PurchaseFailureReason.PurchaseMissing, _fixture.Confirmed[0].FailureReason);
            Assert.AreEqual("Purchase not found", _fixture.Confirmed[0].ErrorMessage);
        }

        [Test]
        public void UnrelatedConfirmationIsIgnored()
        {
            _fixture.Purchases.RaiseConfirmed(StoreFixtures.CreateConfirmedOrder(_fixture.Coins, "t1"));

            Assert.IsEmpty(_fixture.Confirmed);
        }

        [Test]
        public void SubscriptionInfoComesFromConfirmedOrder()
        {
            _fixture.PurchaseAndReceivePending("vip", _fixture.Vip, "t3");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);
            var purchasedProduct = new FakePurchasedProductInfo(ServiceFixture.VipStoreId, StoreFixtures.CreateSubscriptionInfo(ServiceFixture.VipStoreId));

            _fixture.Purchases.RaiseConfirmed(StoreFixtures.CreateConfirmedOrder(_fixture.Vip, "t3", purchasedProduct));

            Assert.IsTrue(_fixture.Service.TryGetSubscriptionInfo("vip", out SubscriptionInfo subscriptionInfo));
            Assert.IsNotNull(subscriptionInfo);
            Assert.IsFalse(_fixture.Service.TryGetSubscriptionInfo("coins", out _));
        }

        [Test]
        public void ConfirmationLostOnDisconnectIsRetriedAfterInitialize()
        {
            _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);
            LogAssert.Expect(LogType.Warning, new Regex("Store disconnected: lost"));
            _fixture.Store.RaiseDisconnected("lost");

            _fixture.Purchases.FetchedOrders = StoreFixtures.CreateOrders(StoreFixtures.CreatePendingOrder(_fixture.Coins, "t1"));
            _fixture.Service.Initialize();

            Assert.IsTrue(_fixture.Service.IsInitialized);
            Assert.AreEqual(2, _fixture.Purchases.ConfirmedOrders.Count);
            Assert.AreEqual(1, _fixture.Purchased.Count);
        }

        [Test]
        public void ConfirmWhileDisconnectedIsIgnored()
        {
            _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");
            LogAssert.Expect(LogType.Warning, new Regex("Store disconnected: lost"));
            _fixture.Store.RaiseDisconnected("lost");

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Confirm coins skipped: service is not initialized.");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            Assert.IsEmpty(_fixture.Purchases.ConfirmedOrders);
        }
    }
}