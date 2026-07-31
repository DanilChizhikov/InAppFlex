using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public sealed class InAppPurchaseService : IInAppPurchaseService, IDisposable
    {
        public event Action OnInitialized;
        public event Action<InitializationFailureException> OnInitializeFailed;
        public event Action<StoreFailureException> OnStoreFailed;
        public event Action<IPurchaseResponse> OnPurchased;
        public event Action<bool> OnPurchasesRestored;
        public event Action<IPurchaseResponse> OnPurchaseFailed;

        private readonly IProductCollection _productCollection;
        private readonly Dictionary<string, bool> _autoConfirmByProductId;
        private readonly Dictionary<string, CancellableCompletion<IPurchaseResponse>> _purchaseCompletionByProductId;

        public bool IsInitialized { get; private set; }

        private StoreController _storeController;
        private CancellableCompletion<bool> _initializeCompletion;
        private CancellableCompletion<bool> _restoreCompletion;

        public InAppPurchaseService(IProductCollection productCollection)
        {
            _productCollection = productCollection;
            _autoConfirmByProductId = new Dictionary<string, bool>();
            _purchaseCompletionByProductId = new Dictionary<string, CancellableCompletion<IPurchaseResponse>>();
        }

        public Task<bool> InitializeAsync(CancellationToken token = default)
        {
            if (IsInitialized)
            {
                return Task.FromResult(true);
            }

            if (_initializeCompletion != null)
            {
                throw new InvalidOperationException($"[{nameof(InAppPurchaseService)}] Initialization is already in progress!");
            }

            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(token);
            }

            if (_productCollection.Count <= 0)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] No products were been added!");
                return Task.FromResult(false);
            }

            _initializeCompletion = new CancellableCompletion<bool>(token);
            _storeController = UnityIAPServices.StoreController();
            Subscribe();

            Debug.Log($"[{nameof(InAppPurchaseService)}] Begin initialize purchasing...");
            ConnectAsync();

            return _initializeCompletion.Task;
        }

        public Task<IPurchaseResponse> PurchaseAsync(string productId, bool autoConfirm = false,
                CancellationToken token = default)
        {
            if (!IsInitialized)
            {
                return Task.FromResult<IPurchaseResponse>(null);
            }

            if (_purchaseCompletionByProductId.ContainsKey(productId))
            {
                throw new InvalidOperationException(
                        $"[{nameof(InAppPurchaseService)}] Purchase of product: {productId} is already in progress!");
            }

            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<IPurchaseResponse>(token);
            }

            if (!TryGetProduct(productId, out Product product) || !product.availableToPurchase)
            {
                return Task.FromResult<IPurchaseResponse>(null);
            }
            
            var completion = new CancellableCompletion<IPurchaseResponse>(token);
            _purchaseCompletionByProductId.Add(product.definition.id, completion);
            _autoConfirmByProductId[product.definition.id] = autoConfirm;
            _storeController.PurchaseProduct(product);

            return completion.Task;
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
            if (!IsInitialized || response is not PurchaseResponse purchaseResponse)
            {
                return;
            }

            PendingOrder pendingOrder = purchaseResponse.PendingOrder;
            if (pendingOrder == null)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Order of product: {response.ProductId} is not pending!");
                return;
            }

            _storeController.ConfirmPurchase(pendingOrder);
        }

        public bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo)
        {
            subscriptionInfo = null;
            if (!IsInitialized || !TryGetProductInfo(productId, out IProductInfo productInfo))
            {
                return false;
            }

            foreach (Order order in _storeController.GetPurchases())
            {
                List<IPurchasedProductInfo> purchasedProducts = order.Info.PurchasedProductInfo;
                for (int i = 0; i < purchasedProducts.Count; i++)
                {
                    IPurchasedProductInfo purchasedProduct = purchasedProducts[i];
                    if (purchasedProduct.productId != productInfo.StoreId || purchasedProduct.subscriptionInfo == null)
                    {
                        continue;
                    }

                    subscriptionInfo = purchasedProduct.subscriptionInfo;
                    return true;
                }
            }

            return false;
        }

        public Task<bool> RestorePurchasesAsync(CancellationToken token = default)
        {
            if (!IsInitialized)
            {
                return Task.FromResult(false);
            }

            if (_restoreCompletion != null)
            {
                throw new InvalidOperationException($"[{nameof(InAppPurchaseService)}] Restoring is already in progress!");
            }

            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(token);
            }

            _restoreCompletion = new CancellableCompletion<bool>(token);
            _storeController.RestoreTransactions(RestorePurchasesCallback);

            return _restoreCompletion.Task;
        }

        public void Dispose()
        {
            CancelPendingOperations();
            if (_storeController != null)
            {
                Unsubscribe();
                _storeController = null;
            }

            _autoConfirmByProductId.Clear();
            IsInitialized = false;
        }

        private void Subscribe()
        {
            _storeController.OnStoreConnected += StoreConnectedHandler;
            _storeController.OnStoreDisconnected += StoreDisconnectedHandler;
            _storeController.OnProductsFetched += ProductsFetchedHandler;
            _storeController.OnProductsFetchFailed += ProductsFetchFailedHandler;
            _storeController.OnPurchasesFetched += PurchasesFetchedHandler;
            _storeController.OnPurchasesFetchFailed += PurchasesFetchFailedHandler;
            _storeController.OnPurchasePending += PurchasePendingHandler;
            _storeController.OnPurchaseConfirmed += PurchaseConfirmedHandler;
            _storeController.OnPurchaseFailed += PurchaseFailedHandler;
        }

        private void Unsubscribe()
        {
            _storeController.OnStoreConnected -= StoreConnectedHandler;
            _storeController.OnStoreDisconnected -= StoreDisconnectedHandler;
            _storeController.OnProductsFetched -= ProductsFetchedHandler;
            _storeController.OnProductsFetchFailed -= ProductsFetchFailedHandler;
            _storeController.OnPurchasesFetched -= PurchasesFetchedHandler;
            _storeController.OnPurchasesFetchFailed -= PurchasesFetchFailedHandler;
            _storeController.OnPurchasePending -= PurchasePendingHandler;
            _storeController.OnPurchaseConfirmed -= PurchaseConfirmedHandler;
            _storeController.OnPurchaseFailed -= PurchaseFailedHandler;
        }

        private async void ConnectAsync()
        {
            try
            {
                await _storeController.Connect();
            }
            catch (Exception exception)
            {
                InitializeFailed(new InitializationFailureException(exception));
            }
        }

        private List<ProductDefinition> GetProductDefinitions()
        {
            var definitions = new List<ProductDefinition>(_productCollection.Count);
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo productInfo = _productCollection[i];
                definitions.Add(new ProductDefinition(productInfo.Id, productInfo.StoreId, productInfo.Type));
                Debug.Log($"[{nameof(InAppPurchaseService)}] Product: {productInfo.StoreId} was been added!");
            }

            return definitions;
        }

        private bool TryGetProductInfo(string id, out IProductInfo productInfo)
        {
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo item = _productCollection[i];
                if (item.Id == id)
                {
                    productInfo = item;
                    return true;
                }
            }

            productInfo = null;
            return false;
        }

        private bool TryGetProduct(string id, out Product product)
        {
            product = null;
            if (_storeController == null || !TryGetProductInfo(id, out _))
            {
                return false;
            }

            product = _storeController.GetProductById(id);
            return product != null;
        }

        private void InitializeCompleted()
        {
            IsInitialized = true;
            CancellableCompletion<bool> completion = _initializeCompletion;
            _initializeCompletion = null;
            Debug.Log($"[{nameof(InAppPurchaseService)}] Purchasing was been initialized!");
            OnInitialized?.Invoke();
            completion?.TrySetResult(true);
        }

        private void InitializeFailed(InitializationFailureException exception)
        {
            Debug.LogException(exception);
            IsInitialized = false;
            CancellableCompletion<bool> completion = _initializeCompletion;
            _initializeCompletion = null;
            OnInitializeFailed?.Invoke(exception);
            completion?.TrySetResult(false);
        }

        private void CompletePurchase(string productId, IPurchaseResponse response)
        {
            if (productId == null ||
                !_purchaseCompletionByProductId.Remove(productId, out CancellableCompletion<IPurchaseResponse> completion))
            {
                return;
            }

            completion.TrySetResult(response);
        }

        private void CancelPendingOperations()
        {
            CancellableCompletion<bool> initializeCompletion = _initializeCompletion;
            _initializeCompletion = null;
            initializeCompletion?.TrySetResult(false);

            CancellableCompletion<bool> restoreCompletion = _restoreCompletion;
            _restoreCompletion = null;
            restoreCompletion?.TrySetResult(false);

            foreach (CancellableCompletion<IPurchaseResponse> completion in _purchaseCompletionByProductId.Values)
            {
                completion.TrySetResult(null);
            }

            _purchaseCompletionByProductId.Clear();
        }

        private void RestorePurchasesCallback(bool result, string errorMessage)
        {
            StoreFailureException exception = null;
            if (result)
            {
                Debug.Log($"[{nameof(InAppPurchaseService)}] Restoring successful!");
            }
            else
            {
                exception = new StoreFailureException(errorMessage);
                Debug.LogException(exception);
            }

            CancellableCompletion<bool> completion = _restoreCompletion;
            _restoreCompletion = null;
            if (exception != null)
            {
                OnStoreFailed?.Invoke(exception);
            }

            OnPurchasesRestored?.Invoke(result);
            completion?.TrySetResult(result);
        }

        private void StoreConnectedHandler()
        {
            Debug.Log($"[{nameof(InAppPurchaseService)}] Store was been connected!");
            _storeController.FetchProducts(GetProductDefinitions());
        }

        private void StoreDisconnectedHandler(StoreConnectionFailureDescription description)
        {
            if (_initializeCompletion != null)
            {
                InitializeFailed(new InitializationFailureException(description));
                return;
            }

            var exception = new StoreFailureException(description);
            Debug.LogException(exception);
            OnStoreFailed?.Invoke(exception);
        }

        private void ProductsFetchedHandler(List<Product> products)
        {
            Debug.Log($"[{nameof(InAppPurchaseService)}] Products was been fetched! Count: {products.Count}");
            if (_initializeCompletion != null)
            {
                InitializeCompleted();
            }
            
            _storeController.FetchPurchases();
        }

        private void ProductsFetchFailedHandler(ProductFetchFailed failure)
        {
            if (_initializeCompletion != null)
            {
                InitializeFailed(new InitializationFailureException(failure));
                return;
            }

            var exception = new StoreFailureException(failure);
            Debug.LogException(exception);
            OnStoreFailed?.Invoke(exception);
        }

        private void PurchasesFetchedHandler(Orders orders)
        {
            Debug.Log($"[{nameof(InAppPurchaseService)}] Purchases was been fetched! " +
                      $"Confirmed: {orders.ConfirmedOrders.Count}, " +
                      $"Pending: {orders.PendingOrders.Count}, " +
                      $"Deferred: {orders.DeferredOrders.Count}");
        }

        private void PurchasesFetchFailedHandler(PurchasesFetchFailureDescription description)
        {
            var exception = new StoreFailureException(description);
            Debug.LogException(exception);
            OnStoreFailed?.Invoke(exception);
        }

        private void PurchasePendingHandler(PendingOrder order)
        {
            Product product = order.CartOrdered?.Items()?.FirstOrDefault()?.Product;
            if (product == null)
            {
                Debug.LogError($"[{nameof(InAppPurchaseService)}] Pending order without any product!");
                return;
            }
            
            string productId = product.definition.id;
            bool autoConfirm = _autoConfirmByProductId.TryGetValue(productId, out bool value) && value;
            _autoConfirmByProductId.Remove(productId);
            var response = new PurchaseResponse(order, product)
            {
                Status = PurchaseStatus.Success,
                IsAutoConfirm = autoConfirm,
            };

            if (autoConfirm)
            {
                _storeController.ConfirmPurchase(order);
            }

            OnPurchased?.Invoke(response);
            CompletePurchase(productId, response);
        }

        private void PurchaseConfirmedHandler(Order order)
        {
            if (order is FailedOrder failedOrder)
            {
                PurchaseFailedHandler(failedOrder);
                return;
            }

            Product product = order.CartOrdered?.Items()?.FirstOrDefault()?.Product;
            Debug.Log($"[{nameof(InAppPurchaseService)}] Purchase was been confirmed! Product: {product?.definition.id}");
        }

        private void PurchaseFailedHandler(FailedOrder order)
        {
            var exception = new PurchaseFailedException(order);
            Debug.LogException(exception);
            string productId = exception.Product?.definition.id;
            if (productId != null)
            {
                _autoConfirmByProductId.Remove(productId);
            }

            var response = new PurchaseResponse(order, exception.Product)
            {
                Status = PurchaseStatus.Failure,
                ErrorMessage = exception.ErrorMessage,
            };

            OnPurchaseFailed?.Invoke(response);
            CompletePurchase(productId, response);
        }
    }
}
