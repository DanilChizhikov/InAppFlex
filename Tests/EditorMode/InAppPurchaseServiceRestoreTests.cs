using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class InAppPurchaseServiceRestoreTests
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
        public void RestoreUsesAvailableAdapterWithPurchaseService()
        {
            _fixture.Service.Initialize();

            _fixture.Service.RestorePurchases();

            Assert.AreEqual(1, _fixture.RestoreAdapter.Calls);
            Assert.AreSame(_fixture.Purchases, _fixture.RestoreAdapter.PurchaseService);
            CollectionAssert.AreEqual(new[] { true }, _fixture.Restored);
        }

        [Test]
        public void RestoreWithoutAvailableAdapterReportsFailure()
        {
            _fixture.RestoreAdapter.IsAvailable = false;
            _fixture.Service.Initialize();

            LogAssert.Expect(LogType.Warning, new Regex("No restore adapter is available"));
            _fixture.Service.RestorePurchases();

            Assert.AreEqual(0, _fixture.RestoreAdapter.Calls);
            CollectionAssert.AreEqual(new[] { false }, _fixture.Restored);
        }
    }
}