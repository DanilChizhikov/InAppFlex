using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class ProductCollectionTests
    {
        private ProductCollection _collection;

        [SetUp]
        public void SetUp()
        {
            _collection = ScriptableObject.CreateInstance<ProductCollection>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_collection);
        }

        [Test]
        public void IsEmptyByDefault()
        {
            Assert.AreEqual(0, _collection.Count);
        }

        [Test]
        public void ExposesSerializedProductsInOrder()
        {
            SerializedObject serializedCollection = new SerializedObject(_collection);
            SerializedProperty products = serializedCollection.FindProperty("_products");
            products.arraySize = 2;
            SetProduct(products.GetArrayElementAtIndex(0), "coins", ProductType.Consumable, "com.game.coins");
            SetProduct(products.GetArrayElementAtIndex(1), "vip", ProductType.Subscription, "com.game.vip");
            serializedCollection.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(2, _collection.Count);
            Assert.AreEqual("coins", _collection[0].Id);
            Assert.AreEqual(ProductType.Consumable, _collection[0].Type);
            Assert.AreEqual("com.game.coins", _collection[0].StoreId);
            Assert.AreEqual("vip", _collection[1].Id);
            Assert.AreEqual(ProductType.Subscription, _collection[1].Type);
            Assert.AreEqual("com.game.vip", _collection[1].StoreId);
        }

        [Test]
        public void ExposesFallbackPriceAndCurrency()
        {
            StoreFixtures.AddProduct(_collection, "noads", ProductType.NonConsumable, "com.game.noads", 4.99f, "EUR");

            Assert.AreEqual(4.99m, _collection[0].DefaultPrice);
            Assert.AreEqual("EUR", _collection[0].DefaultCurrency);
        }

        private static void SetProduct(SerializedProperty product, string id, ProductType type, string storeId)
        {
            product.FindPropertyRelative("<Id>k__BackingField").stringValue = id;
            product.FindPropertyRelative("<Type>k__BackingField").intValue = (int)type;
            product.FindPropertyRelative("<StoreId>k__BackingField").stringValue = storeId;
        }
    }
}