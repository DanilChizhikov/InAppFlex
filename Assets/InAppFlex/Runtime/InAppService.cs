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
        
        private readonly Queue<PurchaseResponse> _purchaseQueue;

        private IStoreController _storeController;

        private bool _isPurchasing;
        
        public bool IsInitialized { get; private set; }

        public InAppService()
        {
            _purchaseQueue = new Queue<PurchaseResponse>();
            _isPurchasing = false;
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

        public IPurchaseResponse Purchase(string productId)
        {
            if (!IsInitialized)
            {
                return new PurchaseResponse
                        {
                                ProductId = productId,
                                Status = PurchaseStatus.Failure,
                                ErrorMessage = "InAppService is not initialized!",
                        };
            }

            if (!TryGetProduct(productId, out Product product))
            {
                return new PurchaseResponse
                        {
                                ProductId = productId,
                                Status = PurchaseStatus.Failure,
                                ErrorMessage = $"Can't find productId = {productId}",
                        };
            }

            if (!product.availableToPurchase)
            {
                return new PurchaseResponse
                        {
                                ProductId = productId,
                                Status = PurchaseStatus.Failure,
                                ErrorMessage = "Product is not available to purchase",
                        };
            }

            var response = new PurchaseResponse();
            response.ProductId = productId;
            response.Status = PurchaseStatus.Processing;
            _purchaseQueue.Enqueue(response);
            TryUpdatePurchaseQueue();
            return response;
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
            if (!TryGetProduct(response.ProductId, out Product product))
            {
                return;
            }
            
            _storeController.ConfirmPendingPurchase(product);
        }
        
        public void Dispose()
        {
            if (!IsInitialized)
            {
                return;
            }
            
            while (_purchaseQueue.TryDequeue(out PurchaseResponse response))
            {
                response.Status = PurchaseStatus.Failure;
                response.ErrorMessage = "InAppService was been disposed";
            }
            
            _storeController = null;
            _isPurchasing = false;
            IsInitialized = false;
        }

        private bool TryGetProduct(string id, out Product product)
        {
            product = _storeController.products.WithID(id);
            return product != null;
        }

        private void TryUpdatePurchaseQueue()
        {
            if (_isPurchasing)
            {
                return;
            }

            if (_purchaseQueue.TryPeek(out PurchaseResponse response) &&
                TryGetProduct(response.ProductId, out Product product))
            {
                _storeController.InitiatePurchase(product);
                _isPurchasing = true;
            }
        }

        private void SendPurchaseEvent(IPurchaseResponse response)
        {
            OnPurchased?.Invoke(response);
        }
        
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
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
            PurchaseResponse response = _purchaseQueue.Dequeue();
            response.TransactionId = purchaseEvent.purchasedProduct.transactionID;
            response.Receipt = purchaseEvent.purchasedProduct.receipt;
            response.Status = PurchaseStatus.Success;
            SendPurchaseEvent(response);
            return PurchaseProcessingResult.Pending;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var logParams = new object[]
                    {
                            product.definition.storeSpecificId,
                            failureReason,
                    };

            PurchaseResponse response = _purchaseQueue.Dequeue();
            response.ErrorMessage = failureReason.ToString();
            response.Status = PurchaseStatus.Failure;
            SendPurchaseEvent(response);
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
            
            PurchaseResponse response = _purchaseQueue.Dequeue();
            response.ErrorMessage = failureDescription.message;
            response.Status = PurchaseStatus.Failure;
            SendPurchaseEvent(response);
            Debug.LogErrorFormat("<color=red>[PurchaseFailed]</color> Product: '{0}', PurchaseFailureReason: {1}, Message: {2}", logParams);
        }
    }
}