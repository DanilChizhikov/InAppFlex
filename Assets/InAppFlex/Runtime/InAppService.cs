using System;
using System.Collections.Generic;
using MbsCore.InAppFlex.Infrastructure;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace MbsCore.InAppFlex.Runtime
{
    public sealed class InAppService : IInAppService, IDetailedStoreListener
    {
        public event Action<IPurchaseResponse> OnPurchased;

        private readonly Queue<string> _purchaseQueue;
        private readonly HashSet<IRestoreAdapter> _restoreAdapters;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;

        private bool _isPurchasing;
        private bool _isRestoring;
        private PurchaseResponse _response;
        
        public bool IsInitialized { get; private set; }

        public InAppService(IEnumerable<IRestoreAdapter> restoreAdapters)
        {
            _purchaseQueue = new Queue<string>();
            _restoreAdapters = new HashSet<IRestoreAdapter>(restoreAdapters);
            _isPurchasing = false;
            _isRestoring = false;
        }
        
        public void Initialize(Dictionary<ProductType, HashSet<string>> products)
        {
            if (IsInitialized)
            {
                return;
            }

            
            if (products.Count <= 0)
            {
                return;
            }
            
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var product in products)
            {
                foreach (var productId in product.Value)
                {
                    builder.AddProduct(productId, product.Key);
                    Debug.Log($"Product: {product.Value} was been added!");
                }
            }
            
            Debug.Log("Begin initialize purchasing...");
            UnityPurchasing.Initialize(this, builder);
        }

        public void Purchase(string productId)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (_isPurchasing || _isRestoring)
            {
                _purchaseQueue.Enqueue(productId);
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

            var response = new PurchaseResponse();
            response.Product = product;
            response.Status = PurchaseStatus.Processing;
            _isPurchasing = true;
            _storeController.InitiatePurchase(product);
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

        public void ConfirmPendingPurchase(IPurchaseResponse response)
        {
            _storeController.ConfirmPendingPurchase(response.Product);
        }

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

                    return true;
                }
                catch
                {
                    Debug.LogError($"<color=red>[{nameof(InAppService)}]</color> No receipt");
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
                    _isRestoring = true;
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
            
            _storeController = null;
            _isPurchasing = false;
            _isRestoring = false;
            IsInitialized = false;
        }

        private bool TryGetProduct(string id, out Product product)
        {
            product = _storeController.products.WithID(id);
            return product != null;
        }

        private void CompletePurchase(ref PurchaseResponse response)
        {
            OnPurchased?.Invoke(response.Clone());
            response = null;
            _isPurchasing = false;
            _isRestoring = false;
            if (_purchaseQueue.TryDequeue(out string productId))
            {
                Purchase(productId);
            }
        }
        
        private void RestorePurchasesCallback(bool result, string errorMessage)
        {
            if (result)
            {
                Debug.Log($"[{nameof(InAppService)}] Restoring successful!");
            }
            else
            {
                Debug.LogError($"<color=red>[{nameof(InAppService)}]</color> Restoring failed! Message: {errorMessage}");
            }
        }

        private PurchaseResponse GetEmptyResponse(Product product)
        {
            var response = new PurchaseResponse();
            response.Product = product;
            response.Status = PurchaseStatus.Processing;
            return response;
        }
        
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            IsInitialized = true;
            Debug.Log("Purchasing was been initialized!");
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogErrorFormat("<color=red>[InitializeFailed]</color> InitializationFailureReason: {0}", error.ToString());
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogErrorFormat("<color=red>[InitializeFailed]</color> InitializationFailureReason: {0}, message: {1}",
                                 error.ToString(), message);
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            if (_response == null)
            {
                _response = GetEmptyResponse(purchaseEvent.purchasedProduct);
            }

            _response.Product = purchaseEvent.purchasedProduct;
            _response.TransactionId = purchaseEvent.purchasedProduct.transactionID;
            _response.Receipt = purchaseEvent.purchasedProduct.receipt;
            _response.Status = PurchaseStatus.Success;
            CompletePurchase(ref _response);
            return PurchaseProcessingResult.Pending;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var logParams = new object[]
                    {
                            product.definition.storeSpecificId,
                            failureReason,
                    };
            
            if (_response == null)
            {
                _response = GetEmptyResponse(product);
            }

            _response.Product = product;
            _response.ErrorMessage = failureReason.ToString();
            _response.Status = PurchaseStatus.Failure;
            CompletePurchase(ref _response);
            Debug.LogErrorFormat("<color=red>[PurchaseFailed]</color> Product: '{0}', PurchaseFailureReason: {1}", logParams);
        }

        void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var logParams = new object[]
                    {
                            product.definition.storeSpecificId,
                            failureDescription.reason,
                            failureDescription.message,
                    };
            
            if (_response == null)
            {
                _response = GetEmptyResponse(product);
            }

            _response.Product = product;
            _response.ErrorMessage = failureDescription.message;
            _response.Status = PurchaseStatus.Failure;
            CompletePurchase(ref _response);
            Debug.LogErrorFormat("<color=red>[PurchaseFailed]</color> Product: '{0}', PurchaseFailureReason: {1}, Message: {2}", logParams);
        }
    }
}