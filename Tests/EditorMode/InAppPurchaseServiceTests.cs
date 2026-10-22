using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class InAppPurchaseServiceTests
    {
        private ProductCollection _collection;
        private CountingRestoreAdapter _restoreAdapter;
        private InAppPurchaseService _service;

        [SetUp]
        public void SetUp()
        {
            _collection = ScriptableObject.CreateInstance<ProductCollection>();
            _restoreAdapter = new CountingRestoreAdapter();
            _service = new InAppPurchaseService(_collection, new IRestoreAdapter[] { _restoreAdapter });
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            Object.DestroyImmediate(_collection);
        }

        [Test]
        public void InitializeWithoutProductsWarnsAndStaysUninitialized()
        {
            bool initialized = false;
            _service.OnInitialized += () => initialized = true;

            LogAssert.Expect(LogType.Warning, $"[{nameof(InAppPurchaseService)}] No products were been added!");
            _service.Initialize();

            Assert.IsFalse(_service.IsInitialized);
            Assert.IsFalse(initialized);
        }

        [Test]
        public void PurchaseBeforeInitializeDoesNothing()
        {
            bool purchased = false;
            bool failed = false;
            _service.OnPurchased += response => purchased = true;
            _service.OnPurchaseFailed += response => failed = true;

            _service.Purchase("coins");

            Assert.IsFalse(purchased);
            Assert.IsFalse(failed);
        }

        [Test]
        public void RestoreBeforeInitializeSkipsAdapters()
        {
            bool restored = false;
            _service.OnPurchasesRestored += result => restored = true;

            _service.RestorePurchases();

            Assert.AreEqual(0, _restoreAdapter.Calls);
            Assert.IsFalse(restored);
        }

        [Test]
        public void SubscriptionInfoIsMissingBeforeInitialize()
        {
            bool found = _service.TryGetSubscriptionInfo("vip", out SubscriptionInfo subscriptionInfo);

            Assert.IsFalse(found);
            Assert.IsNull(subscriptionInfo);
        }

        [Test]
        public void UnknownProductHasFallbackPriceAndCurrency()
        {
            Assert.AreEqual(decimal.MaxValue, _service.GetPrice("unknown"));
            Assert.AreEqual("ERROR", _service.GetStringCurrency("unknown"));
        }

        [Test]
        public void DisposeBeforeInitializeIsSafe()
        {
            Assert.DoesNotThrow(() => _service.Dispose());
            Assert.IsFalse(_service.IsInitialized);
        }
    }
}