using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class DetailedStoreListenerTests
    {
        [Test]
        public void InitializeFailureRaisesEventWithReasonAndMessage()
        {
            DetailedStoreListener listener = new DetailedStoreListener(purchaseEvent => PurchaseProcessingResult.Pending);
            InitializationFailureException received = null;
            listener.OnInitializeFailed += exception => received = exception;

            LogAssert.Expect(LogType.Exception, new Regex("initialization failed: PurchasingUnavailable, message: no store"));
            ((IStoreListener)listener).OnInitializeFailed(InitializationFailureReason.PurchasingUnavailable, "no store");

            Assert.IsNotNull(received);
            StringAssert.Contains("PurchasingUnavailable", received.Message);
            StringAssert.Contains("no store", received.Message);
        }

        [Test]
        public void ProcessPurchaseForwardsToCallback()
        {
            int calls = 0;
            DetailedStoreListener listener = new DetailedStoreListener(purchaseEvent =>
            {
                calls++;
                return PurchaseProcessingResult.Complete;
            });

            PurchaseProcessingResult result = ((IStoreListener)listener).ProcessPurchase(null);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(PurchaseProcessingResult.Complete, result);
        }
    }
}