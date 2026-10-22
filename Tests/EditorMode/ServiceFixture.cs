using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using Object = UnityEngine.Object;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class ServiceFixture : IDisposable
    {
        public const string CoinsStoreId = "com.game.coins";
        public const string VipStoreId = "com.game.vip";
        public const string NoAdsStoreId = "com.game.noads";

        public static LogType StoreFailureLogType => Application.internetReachability == NetworkReachability.NotReachable
                ? LogType.Warning
                : LogType.Error;

        public ProductCollection Collection { get; }
        public FakeStoreService Store { get; } = new();
        public FakeProductService Products { get; } = new();
        public FakePurchaseService Purchases { get; } = new();
        public CountingRestoreAdapter RestoreAdapter { get; } = new();
        public Product Coins { get; }
        public Product Vip { get; }
        public Product NoAds { get; }
        public InAppPurchaseService Service { get; }
        public int InitializedCount { get; private set; }
        public List<InitializationFailureException> InitializeFailures { get; } = new();
        public List<IPurchaseResponse> Purchased { get; } = new();
        public List<IPurchaseResponse> Failed { get; } = new();
        public List<IPurchaseResponse> Deferred { get; } = new();
        public List<IPurchaseResponse> Confirmed { get; } = new();
        public List<bool> Restored { get; } = new();

        public ServiceFixture()
        {
            Collection = ScriptableObject.CreateInstance<ProductCollection>();
            StoreFixtures.AddProduct(Collection, "coins", ProductType.Consumable, CoinsStoreId);
            StoreFixtures.AddProduct(Collection, "vip", ProductType.Subscription, VipStoreId);
            StoreFixtures.AddProduct(Collection, "noads", ProductType.NonConsumable, NoAdsStoreId, 4.99f, "EUR");

            Coins = StoreFixtures.CreateProduct(CoinsStoreId, ProductType.Consumable, 1.99m, "USD");
            Vip = StoreFixtures.CreateProduct(VipStoreId, ProductType.Subscription, 9.99m, "USD");
            NoAds = StoreFixtures.CreateProduct(NoAdsStoreId, ProductType.NonConsumable, 0m, string.Empty);
            Products.AddProduct(Coins);
            Products.AddProduct(Vip);
            Products.AddProduct(NoAds);

            Service = new InAppPurchaseService(Collection, new IRestoreAdapter[] { RestoreAdapter }, Store, Products, Purchases);
            Service.OnInitialized += () => InitializedCount++;
            Service.OnInitializeFailed += exception => InitializeFailures.Add(exception);
            Service.OnPurchased += response => Purchased.Add(response);
            Service.OnPurchaseFailed += response => Failed.Add(response);
            Service.OnPurchaseDeferred += response => Deferred.Add(response);
            Service.OnPurchaseConfirmed += response => Confirmed.Add(response);
            Service.OnPurchasesRestored += result => Restored.Add(result);
        }

        public PendingOrder PurchaseAndReceivePending(string productId, Product product, string transactionId, bool autoConfirm = false)
        {
            PendingOrder order = StoreFixtures.CreatePendingOrder(product, transactionId);
            Service.Purchase(productId, autoConfirm);
            Purchases.RaisePending(order);
            return order;
        }

        public void Dispose()
        {
            Service.Dispose();
            Object.DestroyImmediate(Collection);
        }
    }
}