using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class InAppPurchaseServiceQueueTests
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
        public void PurchaseUsesStoreProductOfCollectionId()
        {
            _fixture.Service.Purchase("coins");

            Assert.AreEqual(1, _fixture.Purchases.PurchasedProducts.Count);
            Assert.AreSame(_fixture.Coins, _fixture.Purchases.PurchasedProducts[0]);
        }

        [Test]
        public void UnknownProductIsNotPurchased()
        {
            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Purchase gems skipped: product not found.");
            _fixture.Service.Purchase("gems");

            Assert.IsEmpty(_fixture.Purchases.PurchasedProducts);
        }

        [Test]
        public void PurchasesAreProcessedOneAtATime()
        {
            _fixture.Service.Purchase("coins");
            _fixture.Service.Purchase("noads");

            Assert.AreEqual(1, _fixture.Purchases.PurchasedProducts.Count);

            _fixture.Purchases.RaisePending(StoreFixtures.CreatePendingOrder(_fixture.Coins, "t1"));

            Assert.AreEqual(2, _fixture.Purchases.PurchasedProducts.Count);
            Assert.AreSame(_fixture.NoAds, _fixture.Purchases.PurchasedProducts[1]);
            Assert.AreEqual("coins", _fixture.Purchased[0].ProductId);
            Assert.IsFalse(_fixture.Purchased[0].IsRestored);
        }

        [Test]
        public void AutoConfirmConfirmsBeforeRaisingPurchased()
        {
            int confirmedBeforeEvent = -1;
            _fixture.Service.OnPurchased += response => confirmedBeforeEvent = _fixture.Purchases.ConfirmedOrders.Count;

            PendingOrder order = _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1", true);

            Assert.AreEqual(1, confirmedBeforeEvent);
            Assert.AreSame(order, _fixture.Purchases.ConfirmedOrders[0]);
            Assert.IsTrue(_fixture.Purchased[0].IsAutoConfirm);
        }

        [Test]
        public void ManualConfirmWaitsForConsumer()
        {
            PendingOrder order = _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");

            Assert.IsEmpty(_fixture.Purchases.ConfirmedOrders);

            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            Assert.AreEqual(1, _fixture.Purchases.ConfirmedOrders.Count);
            Assert.AreSame(order, _fixture.Purchases.ConfirmedOrders[0]);
        }

        [Test]
        public void SecondConfirmIsIgnored()
        {
            _fixture.PurchaseAndReceivePending("coins", _fixture.Coins, "t1");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Pending purchase coins is already confirmed or unknown.");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Purchased[0]);

            Assert.AreEqual(1, _fixture.Purchases.ConfirmedOrders.Count);
        }

        [Test]
        public void FailedPurchaseDoesNotBlockQueue()
        {
            _fixture.Service.Purchase("coins");
            _fixture.Service.Purchase("noads");

            LogAssert.Expect(LogType.Exception, new Regex("Product: coins, PurchaseFailureReason: PaymentDeclined"));
            _fixture.Purchases.RaiseFailed(new FailedOrder(new Cart(new CartItem(_fixture.Coins)), PurchaseFailureReason.PaymentDeclined, "declined"));

            Assert.AreEqual(1, _fixture.Failed.Count);
            Assert.AreEqual(PurchaseStatus.Failure, _fixture.Failed[0].Status);
            Assert.AreEqual(PurchaseFailureReason.PaymentDeclined, _fixture.Failed[0].FailureReason);
            Assert.AreEqual("declined", _fixture.Failed[0].ErrorMessage);
            Assert.AreEqual(2, _fixture.Purchases.PurchasedProducts.Count);
        }

        [Test]
        public void FailedOrderWithoutCartFailsCurrentPurchase()
        {
            _fixture.Service.Purchase("coins");

            LogAssert.Expect(LogType.Exception, new Regex("Product: coins, PurchaseFailureReason: ProductUnavailable"));
            _fixture.Purchases.RaiseFailed(new FailedOrder(new EmptyCart(), PurchaseFailureReason.ProductUnavailable, "unavailable"));
            _fixture.Service.Purchase("noads");

            Assert.AreEqual("coins", _fixture.Failed[0].ProductId);
            Assert.AreEqual(2, _fixture.Purchases.PurchasedProducts.Count);
        }

        [Test]
        public void CancelledPurchaseIsLoggedAsInfo()
        {
            _fixture.Service.Purchase("coins");

            LogAssert.Expect(LogType.Log, $"[{nameof(InAppPurchaseService)}] Purchase coins cancelled by user.");
            _fixture.Purchases.RaiseFailed(new FailedOrder(new Cart(new CartItem(_fixture.Coins)), PurchaseFailureReason.UserCancelled, "cancelled"));

            Assert.AreEqual(PurchaseFailureReason.UserCancelled, _fixture.Failed[0].FailureReason);
        }

        [Test]
        public void DeferredPurchaseRaisesDeferredAndContinuesQueue()
        {
            _fixture.Service.Purchase("coins");
            _fixture.Service.Purchase("noads");

            _fixture.Purchases.RaiseDeferred(new DeferredOrder(new Cart(new CartItem(_fixture.Coins)), new FakeOrderInfo(string.Empty)));

            Assert.AreEqual(1, _fixture.Deferred.Count);
            Assert.AreEqual(PurchaseStatus.Deferred, _fixture.Deferred[0].Status);
            Assert.AreEqual("coins", _fixture.Deferred[0].ProductId);
            Assert.AreEqual(2, _fixture.Purchases.PurchasedProducts.Count);

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] Purchase coins has no pending order to confirm.");
            _fixture.Service.ConfirmPendingPurchase(_fixture.Deferred[0]);

            Assert.IsEmpty(_fixture.Purchases.ConfirmedOrders);
        }

        [Test]
        public void PendingOrderOutsideQueueIsRestored()
        {
            _fixture.Service.Purchase("coins");

            _fixture.Purchases.RaisePending(StoreFixtures.CreatePendingOrder(_fixture.NoAds, "t9"));

            Assert.AreEqual(1, _fixture.Purchased.Count);
            Assert.AreEqual("noads", _fixture.Purchased[0].ProductId);
            Assert.IsTrue(_fixture.Purchased[0].IsRestored);
            Assert.AreEqual(1, _fixture.Purchases.PurchasedProducts.Count);
        }

        [Test]
        public void DisconnectFailsCurrentAndQueuedPurchases()
        {
            _fixture.Service.Purchase("coins");
            _fixture.Service.Purchase("noads");

            LogAssert.Expect(LogType.Exception, new Regex("Product: coins, PurchaseFailureReason: StoreNotConnected"));
            LogAssert.Expect(LogType.Exception, new Regex("Product: noads, PurchaseFailureReason: StoreNotConnected"));
            LogAssert.Expect(LogType.Warning, new Regex("Store disconnected: lost"));
            _fixture.Store.RaiseDisconnected("lost");

            Assert.AreEqual(2, _fixture.Failed.Count);
            Assert.AreEqual(PurchaseFailureReason.StoreNotConnected, _fixture.Failed[1].FailureReason);
            Assert.IsFalse(_fixture.Service.IsInitialized);
            Assert.AreEqual(1, _fixture.InitializeFailures.Count);
        }

        [Test]
        public void PriceAndCurrencyComeFromStoreMetadata()
        {
            Assert.AreEqual(1.99m, _fixture.Service.GetPrice("coins"));
            Assert.AreEqual("USD", _fixture.Service.GetStringCurrency("coins"));
        }

        [Test]
        public void PriceAndCurrencyFallBackToProductDefaults()
        {
            Assert.AreEqual(4.99m, _fixture.Service.GetPrice("noads"));
            Assert.AreEqual("EUR", _fixture.Service.GetStringCurrency("noads"));
        }
    }
}