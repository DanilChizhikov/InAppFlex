using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public static class StoreFixtures
    {
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static Product CreateProduct(string storeId, ProductType type, decimal price, string currency)
        {
            var definition = new ProductDefinition(storeId, type);
            var metadata = new ProductMetadata(price.ToString(CultureInfo.InvariantCulture), storeId, storeId, currency, price);
            var product = (Product)Activator.CreateInstance(typeof(Product), NonPublicInstance, null, new object[] { definition, metadata }, null);
            typeof(Product).GetProperty(nameof(Product.availableToPurchase)).SetValue(product, true);
            return product;
        }

        public static ProductFetchFailed CreateProductFetchFailed(List<ProductDefinition> products, string reason)
        {
            return (ProductFetchFailed)Activator.CreateInstance(typeof(ProductFetchFailed), NonPublicInstance, null, new object[] { products, reason }, null);
        }

        public static PendingOrder CreatePendingOrder(Product product, string transactionId)
        {
            return new PendingOrder(new Cart(new CartItem(product)), new FakeOrderInfo(transactionId));
        }

        public static ConfirmedOrder CreateConfirmedOrder(Product product, string transactionId, params IPurchasedProductInfo[] purchasedProducts)
        {
            var info = new FakeOrderInfo(transactionId);
            var order = new ConfirmedOrder(new Cart(new CartItem(product)), info);
            info.PurchasedProductInfo = new List<IPurchasedProductInfo>(purchasedProducts);
            return order;
        }

        public static Orders CreateOrders(params PendingOrder[] pendingOrders)
        {
            return new Orders(new List<ConfirmedOrder>(), new List<PendingOrder>(pendingOrders), new List<DeferredOrder>());
        }

        public static SubscriptionInfo CreateSubscriptionInfo(string storeId)
        {
            string skuDetails = "{\"type\":\"subs\",\"productId\":\"" + storeId + "\",\"subscriptionPeriod\":\"P1M\"}";
            return new SubscriptionInfo(skuDetails, true, DateTime.UtcNow, false, false, false, null);
        }

        public static void AddProduct(ProductCollection collection, string id, ProductType type, string storeId,
                                      float defaultPrice = 0f, string defaultCurrency = "")
        {
            var serializedCollection = new SerializedObject(collection);
            SerializedProperty products = serializedCollection.FindProperty("_products");
            products.arraySize++;
            SerializedProperty product = products.GetArrayElementAtIndex(products.arraySize - 1);
            product.FindPropertyRelative("<Id>k__BackingField").stringValue = id;
            product.FindPropertyRelative("<Type>k__BackingField").intValue = (int)type;
            product.FindPropertyRelative("<StoreId>k__BackingField").stringValue = storeId;
            product.FindPropertyRelative("<DefaultPrice>k__BackingField").doubleValue = defaultPrice;
            product.FindPropertyRelative("<DefaultCurrency>k__BackingField").stringValue = defaultCurrency;
            serializedCollection.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}