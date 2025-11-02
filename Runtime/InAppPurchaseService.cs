using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public sealed class InAppPurchaseService : IInAppPurchaseService, IDetailedStoreListener
    {
        public event Action OnInitialized;
        public event Action<InitializationFailureReason> OnInitializedFailed;
        public event Action<bool> OnPurchasesRestored;
        public event Action<IPurchaseResponse> OnPurchased;
        
        private readonly HashSet<IRestoreAdapter> _restoreAdapters;
        private readonly Dictionary<string, Queue<PurchaseResponse>> _responseMap;

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        
        public bool IsInitialized { get; private set; }

        public InAppPurchaseService(IEnumerable<IRestoreAdapter> restoreAdapters)
        {
            _restoreAdapters = new HashSet<IRestoreAdapter>(restoreAdapters);
            _responseMap = new Dictionary<string, Queue<PurchaseResponse>>();
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
                foreach (string productId in product.Value)
                {
                    builder.AddProduct(productId, product.Key);
                    Debug.Log($"[{nameof(InAppPurchaseService)}] Product: {product.Value} was been added!");
                }
            }
            
            Debug.Log($"[{nameof(InAppPurchaseService)}] Begin initialize purchasing...");
            UnityPurchasing.Initialize(this, builder);
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

            var response = new PurchaseResponse();
            response.Product = product;
            response.Status = PurchaseStatus.Processing;
            response.CanConfirm = autoConfirm;
            if (!_responseMap.TryGetValue(productId, out Queue<PurchaseResponse> responses))
            {
                responses = new Queue<PurchaseResponse>();
                _responseMap.Add(productId, responses);
            }

            responses.Enqueue(response);
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
                    return true;
                }
                catch
                {
                    Debug.LogError($"<color=red>[{nameof(InAppPurchaseService)}]</color> No receipt");
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
            
            _storeController = null;
            IsInitialized = false;
        }

        private bool TryGetProduct(string id, out Product product)
        {
            product = _storeController.products.WithID(id);
            return product != null;
        }

        private void CompletePurchase(PurchaseResponse response)
        {
            IPurchaseResponse clone = response.Clone();
            if (response.CanConfirm)
            {
                ConfirmPendingPurchase(clone);
            }
            
            OnPurchased?.Invoke(clone);
        }
        
        private void RestorePurchasesCallback(bool result, string errorMessage)
        {
            if (result)
            {
                Debug.Log($"[{nameof(InAppPurchaseService)}] Restoring successful!");
            }
            else
            {
                Debug.LogError($"<color=red>[{nameof(InAppPurchaseService)}]</color> Restoring failed! Message: {errorMessage}");
            }

            OnPurchasesRestored?.Invoke(result);
        }

        private bool TryGetResponse(string productId, out PurchaseResponse response)
        {
            response = new PurchaseResponse
            {
                Product = TryGetProduct(productId, out Product product)? product : null,
                Status = PurchaseStatus.Processing,
            };

            return _responseMap.TryGetValue(productId, out Queue<PurchaseResponse> responses) && responses.TryDequeue(out response);
        }
        
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            IsInitialized = true;
            OnInitialized?.Invoke();
            Debug.Log("Purchasing was been initialized!");
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogErrorFormat("<color=red>[{0}]</color> InitializationFailureReason: {1}", nameof(InAppPurchaseService), error.ToString());
            OnInitializedFailed?.Invoke(error);
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogErrorFormat("<color=red>[{0}]</color> InitializationFailureReason: {1}, message: {2}",
                                nameof(InAppPurchaseService), error.ToString(), message);
            OnInitializedFailed?.Invoke(error);
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            PurchaseProcessingResult result = PurchaseProcessingResult.Pending;
            if (!TryGetResponse(purchaseEvent.purchasedProduct.definition.id, out PurchaseResponse response))
            {
                Debug.LogError($"[{nameof(InAppPurchaseService)}] No response for product: {purchaseEvent.purchasedProduct.definition.id}!");
                result = PurchaseProcessingResult.Complete;
            }

            if (result != PurchaseProcessingResult.Complete)
            {
                response.Product = purchaseEvent.purchasedProduct;
                response.TransactionId = purchaseEvent.purchasedProduct.transactionID;
                response.Receipt = purchaseEvent.purchasedProduct.receipt;
                response.Status = PurchaseStatus.Success;
                CompletePurchase( response );
            }

            return result;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var logParams = new object[]
                    {
                            product.definition.storeSpecificId,
                            failureReason,
                    };

            if (TryGetResponse(product.definition.id, out PurchaseResponse response))
            {
                response.Product = product;
                response.ErrorMessage = failureReason.ToString();
                response.Status = PurchaseStatus.Failure;
                CompletePurchase(response);
            }
            
            Debug.LogErrorFormat("<color=red>[{0}]</color> OnPurchaseFailed\n Product: '{1}', PurchaseFailureReason: {2}",
                nameof(InAppPurchaseService), product.receipt, logParams);
        }

        void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var logParams = new object[]
                    {
                            product.definition.storeSpecificId,
                            failureDescription.reason,
                            failureDescription.message,
                    };
            
            if (TryGetResponse(product.definition.id, out PurchaseResponse response))
            {
                response.Product = product;
                response.ErrorMessage = failureDescription.message;
                response.Status = PurchaseStatus.Failure;
                CompletePurchase(response);
            }
            
            Debug.LogErrorFormat("<color=red>[{0}]</color> OnPurchaseFailed\n Product: '{1}', PurchaseFailureReason: {2}, Message: {3}",
                nameof(InAppPurchaseService), product.receipt, logParams);
        }
    }
}