using NUnit.Framework;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class OrderReceiptReaderTests
    {
        [Test]
        public void GooglePlayOrderIdReplacesPurchaseToken()
        {
            PendingOrder order = CreateOrder("token", CreateGooglePlayReceipt("{\"orderId\":\"GPA.1234\",\"quantity\":3}"));

            Assert.AreEqual("GPA.1234", OrderReceiptReader.GetTransactionId(order));
            Assert.AreEqual(3, OrderReceiptReader.GetQuantity(order, order.CartOrdered.Items()[0]));
        }

        [Test]
        public void OtherStoreKeepsTransactionId()
        {
            PendingOrder order = CreateOrder("t1", "{\"Store\":\"AppleAppStore\",\"TransactionID\":\"t1\",\"Payload\":\"\"}");

            Assert.AreEqual("t1", OrderReceiptReader.GetTransactionId(order));
            Assert.AreEqual(1, OrderReceiptReader.GetQuantity(order, order.CartOrdered.Items()[0]));
        }

        [Test]
        public void BrokenReceiptFallsBackToOrderData()
        {
            PendingOrder order = CreateOrder("t1", "not a receipt");

            Assert.AreEqual("t1", OrderReceiptReader.GetTransactionId(order));
            Assert.AreEqual(1, OrderReceiptReader.GetQuantity(order, null));
        }

        [Test]
        public void SignedTransactionComesFromAppleOrderInfo()
        {
            var info = new FakeOrderInfo("t1") { jwsRepresentation = "header.payload.signature" };
            var order = new PendingOrder(new Cart(new CartItem(CreateProduct())), info);

            Assert.AreEqual("header.payload.signature", OrderReceiptReader.GetSignedTransaction(order));
            Assert.AreEqual(string.Empty, OrderReceiptReader.GetSignedTransaction(CreateOrder("t2", string.Empty)));
        }

        [Test]
        public void LogReferenceHidesTransactionId()
        {
            string reference = OrderReceiptReader.GetLogReference("secret-token");

            StringAssert.StartsWith("sha256:", reference);
            Assert.AreEqual(23, reference.Length);
            StringAssert.DoesNotContain("secret-token", reference);
            Assert.AreEqual(reference, OrderReceiptReader.GetLogReference("secret-token"));
            Assert.AreEqual("none", OrderReceiptReader.GetLogReference(string.Empty));
        }

        private static Product CreateProduct()
        {
            return StoreFixtures.CreateProduct("com.game.coins", ProductType.Consumable, 1m, "USD");
        }

        private static PendingOrder CreateOrder(string transactionId, string receipt)
        {
            return new PendingOrder(new Cart(new CartItem(CreateProduct())), new FakeOrderInfo(transactionId, receipt));
        }

        private static string CreateGooglePlayReceipt(string purchaseJson)
        {
            string payload = "{\"json\":" + Quote(purchaseJson) + ",\"signature\":\"signature\"}";
            return "{\"Store\":\"GooglePlay\",\"TransactionID\":\"token\",\"Payload\":" + Quote(payload) + "}";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}