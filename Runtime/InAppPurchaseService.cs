using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace DTech.InAppFlex
{
    public sealed class InAppPurchaseService : IInAppPurchaseService, IDisposable
    {
        public event Action OnInitialized;
        public event Action<InitializationFailureException> OnInitializeFailed;
        public event Action<IPurchaseResponse> OnPurchased;
        public event Action<bool> OnPurchasesRestored;
        public event Action<IPurchaseResponse> OnPurchaseFailed;

        private readonly IProductCollection _productCollection;
        private readonly DetailedStoreListener _storeListener;
        private readonly HashSet<IRestoreAdapter> _restoreAdapters;
        private readonly Queue<PurchaseQueueItem> _purchaseQueue;
        
        public bool IsInitialized { get; private set; }
        
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private bool _isInitializeProcessing;
        private PurchaseQueueItem _currentItem;
        private bool _isPurchaseProcessing;

        public InAppPurchaseService(IProductCollection productCollection, IEnumerable<IRestoreAdapter> restoreAdapters)
        {
            _productCollection = productCollection;
            _storeListener = new DetailedStoreListener(ProcessPurchase);
            _restoreAdapters = new HashSet<IRestoreAdapter>(restoreAdapters);
            _purchaseQueue = new Queue<PurchaseQueueItem>();
            
            _storeListener.OnInitialized += InitializedHandler;
            _storeListener.OnInitializeFailed += InitializeFailedHandler;
            _storeListener.OnPurchaseFailed += PurchaseFailedHandler;
        }

        public void Initialize()
        {
            if (IsInitialized || _isInitializeProcessing)
            {
                return;
            }

            if (_productCollection.Count <= 0)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] No products were been added!");
                return;
            }

            IPurchasingModule purchasingModule = StandardPurchasingModule.Instance();
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(purchasingModule);
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo productInfo = _productCollection[i];
                builder.AddProduct(productInfo.StoreId, productInfo.Type);
                Debug.Log($"[{nameof(InAppPurchaseService)}] Product: {productInfo.StoreId} was been added!");
            }
            
            Debug.Log($"[{nameof(InAppPurchaseService)}] Begin initialize purchasing...");
            UnityPurchasing.Initialize(_storeListener, builder);
            _isInitializeProcessing = true;
        }

        public void Purchase(string productId, bool autoConfirm = false)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (!TryGetProduct(productId, out Product product))
            {
                return;
            }

            if (!product.availableToPurchase)
            {
                return;
            }
            
            _purchaseQueue.Enqueue(new PurchaseQueueItem(product.definition.id, autoConfirm));
            TryInitializePurchase();
        }

        public decimal GetPrice(string productId)
        {
            decimal price = TryGetProduct(productId, out Product product)
                                    ? product.metadata.localizedPrice
                                    : decimal.MaxValue;

            return Math.Round(price, 2);
        }

        public string GetStringCurrency(string productId) =>
                TryGetProduct(productId, out Product product) ? product.metadata.isoCurrencyCode : "ERROR";

        public void ConfirmPendingPurchase(IPurchaseResponse response) => _storeController.ConfirmPendingPurchase(response.Product);

        public bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo)
        {
            subscriptionInfo = null;
            if (!IsInitialized)
            {
                return false;
            }

            Product[] products = _storeController.products.all;
            for (int i = 0; i < products.Length; i++)
            {
                Product item = products[i];
                if (item.definition.id != productId || !item.hasReceipt)
                {
                    continue;
                }

                var subscriptionManager = new SubscriptionManager(item, null);
                try
                {
                    subscriptionInfo = subscriptionManager.getSubscriptionInfo();
                    return subscriptionInfo != null;
                }
                catch
                {
                    Debug.LogError($"[{nameof(InAppPurchaseService)}] No receipt for product: {productId}");
                }
            }

            return false;
        }

        public void RestorePurchases()
        {
            if (!IsInitialized)
            {
                return;
            }

            foreach (var adapter in _restoreAdapters)
            {
                if (adapter.IsAvailable)
                {
                    adapter.RestorePurchases(_extensionProvider, RestorePurchasesCallback);
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (!IsInitialized)
            {
                return;
            }
            
            _storeListener.OnInitialized -= InitializedHandler;
            _storeListener.OnInitializeFailed -= InitializeFailedHandler;
            _storeListener.OnPurchaseFailed -= PurchaseFailedHandler;
            _storeController = null;
            IsInitialized = false;
        }
        
        private void TryInitializePurchase()
        {
            if (_isInitializeProcessing)
            {
                return;
            }

            if (_purchaseQueue.TryDequeue(out PurchaseQueueItem item))
            {
                _currentItem = item;
                _isPurchaseProcessing = true;
                _storeController.InitiatePurchase(item.ProductId);
            }
        }

        private bool TryGetProduct(string id, out Product product)
        {
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo productInfo = _productCollection[i];
                if (productInfo.Id == id)
                {
                    product = _storeController.products.WithID(id);
                    return product != null;
                }
            }

            product = null;
            return false;
        }

        private void CompletePurchase(PurchaseResponse response)
        {
            if (response.IsAutoConfirm)
            {
                ConfirmPendingPurchase(response);
            }

            _currentItem = default;
            _isPurchaseProcessing = false;
            OnPurchased?.Invoke(response);
            TryInitializePurchase();
        }
        
        private void RestorePurchasesCallback(bool result, string errorMessage)
        {
            if (result)
            {
                Debug.Log($"[{nameof(InAppPurchaseService)}] Restoring successful!");
            }
            else
            {
                Debug.LogError($"[{nameof(InAppPurchaseService)}] Restoring failed! Message: {errorMessage}");
            }

            OnPurchasesRestored?.Invoke(result);
        }
        
        private PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            PurchaseProcessingResult result = PurchaseProcessingResult.Pending;
            if (_currentItem.ProductId == purchaseEvent.purchasedProduct.definition.id)
            {
                var response = new PurchaseResponse(purchaseEvent.purchasedProduct)
                {
                    Status = PurchaseStatus.Success,
                    IsAutoConfirm = _currentItem.AutoConfirm,
                };
                
                CompletePurchase(response);
                result = PurchaseProcessingResult.Complete;
            }

            return result;
        }
        
        private void InitializedHandler(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            IsInitialized = true;
            OnInitialized?.Invoke();
            Debug.Log("Purchasing was been initialized!");
        }
        
        private void InitializeFailedHandler(InitializationFailureException exception)
        {
            _isInitializeProcessing = false;
            IsInitialized = false;
            OnInitializeFailed?.Invoke(exception);
        }
        
        private void PurchaseFailedHandler(PurchaseFailedException exception)
        {
            var response = new PurchaseResponse(exception.Product)
            {
                Status = PurchaseStatus.Failure,
                ErrorMessage = exception.ErrorMessage,
                IsAutoConfirm = _currentItem.AutoConfirm,
            };
            
            OnPurchaseFailed?.Invoke(response);
            if (_isPurchaseProcessing && exception.Product.definition.id == _currentItem.ProductId)
            {
                _currentItem = default;
                _isPurchaseProcessing = false;
                TryInitializePurchase();
            }
        }
    }
}