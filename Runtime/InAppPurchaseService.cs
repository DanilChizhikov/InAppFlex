using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public sealed class InAppPurchaseService : IInAppPurchaseService, IDisposable
    {
        public event Action OnInitialized;
        public event Action<InitializationFailureException> OnInitializeFailed;
        public event Action<IPurchaseResponse> OnPurchased;
        public event Action<bool> OnPurchasesRestored;
        public event Action<IPurchaseResponse> OnPurchaseFailed;
        public event Action<IPurchaseResponse> OnPurchaseDeferred;
        public event Action<IPurchaseResponse> OnPurchaseConfirmed;

        private const string MissingCurrency = "ERROR";

        private readonly IProductCollection _productCollection;
        private readonly HashSet<IRestoreAdapter> _restoreAdapters;
        private readonly Queue<PurchaseQueueItem> _purchaseQueue;
        private readonly Dictionary<string, IProductInfo> _productsByStoreId;
        private readonly Dictionary<string, PurchaseResponse> _pendingResponses;
        private readonly Dictionary<string, PurchaseResponse> _confirmingResponses;
        private readonly Dictionary<string, SubscriptionInfo> _subscriptions;

        public bool IsInitialized { get; private set; }

        private IStoreService _storeService;
        private IProductService _productService;
        private IPurchaseService _purchaseService;
        private bool _isSubscribed;
        private InitializationStage _initializationStage;
        private string _disconnectMessage;
        private PurchaseQueueItem _currentItem;
        private bool _isPurchaseProcessing;
        private bool _isRestoreProcessing;

        public InAppPurchaseService(IProductCollection productCollection, IEnumerable<IRestoreAdapter> restoreAdapters)
        {
            _productCollection = productCollection;
            _restoreAdapters = new HashSet<IRestoreAdapter>(restoreAdapters);
            _purchaseQueue = new Queue<PurchaseQueueItem>();
            _productsByStoreId = new Dictionary<string, IProductInfo>();
            _pendingResponses = new Dictionary<string, PurchaseResponse>();
            _confirmingResponses = new Dictionary<string, PurchaseResponse>();
            _subscriptions = new Dictionary<string, SubscriptionInfo>();
        }

        internal InAppPurchaseService(IProductCollection productCollection, IEnumerable<IRestoreAdapter> restoreAdapters,
                                      IStoreService storeService, IProductService productService, IPurchaseService purchaseService)
                : this(productCollection, restoreAdapters)
        {
            _storeService = storeService;
            _productService = productService;
            _purchaseService = purchaseService;
        }

        public void Initialize()
        {
            if (IsInitialized || _initializationStage != InitializationStage.None)
            {
                return;
            }

            if (_productCollection.Count <= 0)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] No products were been added!");
                return;
            }

            if (_storeService == null)
            {
                StoreController storeController = UnityIAPServices.StoreController();
                _storeService = storeController;
                _productService = storeController;
                _purchaseService = storeController;
            }

            Subscribe();
            _productsByStoreId.Clear();
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo productInfo = _productCollection[i];
                if (string.IsNullOrEmpty(productInfo.StoreId))
                {
                    Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Product: {productInfo.Id} has no store id and is skipped.");
                    continue;
                }

                _productsByStoreId[productInfo.StoreId] = productInfo;
                Debug.Log($"[{nameof(InAppPurchaseService)}] Product: {productInfo.StoreId} was been added!");
            }

            Debug.Log($"[{nameof(InAppPurchaseService)}] Begin initialize purchasing...");
            _purchaseService.ProcessPendingOrdersOnPurchasesFetched(false);
            _disconnectMessage = null;
            _initializationStage = InitializationStage.Connecting;
            try
            {
                _storeService.Connect();
            }
            catch (Exception exception)
            {
                FailInitialization($"Connect failed: {exception.Message}");
            }
        }

        public void Purchase(string productId, bool autoConfirm = false)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase {productId} skipped: service is not initialized.");
                return;
            }

            IProductInfo productInfo = FindProductInfo(productId);
            Product product = GetStoreProduct(productInfo);
            if (product == null)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase {productId} skipped: product not found.");
                return;
            }

            if (!product.availableToPurchase)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase {productId} skipped: product is not available to purchase.");
                return;
            }

            _purchaseQueue.Enqueue(new PurchaseQueueItem(productInfo, product, autoConfirm));
            TryProcessNextPurchase();
        }

        public decimal GetPrice(string productId)
        {
            IProductInfo productInfo = FindProductInfo(productId);
            Product product = GetStoreProduct(productInfo);
            decimal price = decimal.MaxValue;
            if (product != null && product.metadata.localizedPrice > 0)
            {
                price = product.metadata.localizedPrice;
            }
            else if (productInfo != null && productInfo.DefaultPrice > 0)
            {
                price = (decimal)productInfo.DefaultPrice;
            }

            return Math.Round(price, 2);
        }

        public string GetStringCurrency(string productId)
        {
            IProductInfo productInfo = FindProductInfo(productId);
            Product product = GetStoreProduct(productInfo);
            if (product != null && !string.IsNullOrEmpty(product.metadata.isoCurrencyCode))
            {
                return product.metadata.isoCurrencyCode;
            }

            return productInfo != null && !string.IsNullOrEmpty(productInfo.DefaultCurrency)
                    ? productInfo.DefaultCurrency
                    : MissingCurrency;
        }

        public void ConfirmPendingPurchase(IPurchaseResponse response)
        {
            if (!(response is PurchaseResponse purchaseResponse) || purchaseResponse.Order == null)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase {response?.ProductId} has no pending order to confirm.");
                return;
            }

            if (!IsInitialized)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Confirm {purchaseResponse.ProductId} skipped: service is not initialized.");
                return;
            }

            string key = GetOrderKey(purchaseResponse.Order);
            if (!_pendingResponses.Remove(key))
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Pending purchase {purchaseResponse.ProductId} is already confirmed or unknown.");
                return;
            }

            _confirmingResponses[key] = purchaseResponse;
            _purchaseService.ConfirmPurchase(purchaseResponse.Order);
        }

        public bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo)
        {
            subscriptionInfo = null;
            if (!IsInitialized)
            {
                return false;
            }

            IProductInfo productInfo = FindProductInfo(productId);
            return !string.IsNullOrEmpty(productInfo?.StoreId) &&
                   _subscriptions.TryGetValue(productInfo.StoreId, out subscriptionInfo) &&
                   subscriptionInfo != null;
        }

        public void RestorePurchases()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (_isRestoreProcessing)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Restoring is already in progress.");
                return;
            }

            foreach (var adapter in _restoreAdapters)
            {
                if (adapter.IsAvailable)
                {
                    _isRestoreProcessing = true;
                    adapter.RestorePurchases(_purchaseService, RestorePurchasesCallback);
                    return;
                }
            }

            Debug.LogWarning($"[{nameof(InAppPurchaseService)}] No restore adapter is available on {Application.platform}.");
            OnPurchasesRestored?.Invoke(false);
        }

        public void Dispose()
        {
            Unsubscribe();
            _purchaseQueue.Clear();
            _pendingResponses.Clear();
            _confirmingResponses.Clear();
            _subscriptions.Clear();
            _currentItem = default;
            _isPurchaseProcessing = false;
            _isRestoreProcessing = false;
            _initializationStage = InitializationStage.None;
            IsInitialized = false;
        }

        private static CartItem GetCartItem(Order order)
        {
            IReadOnlyList<CartItem> items = order?.CartOrdered?.Items();
            return items != null && items.Count > 0 ? items[0] : null;
        }

        private static string GetStoreId(Order order)
        {
            string storeId = GetCartItem(order)?.Product?.definition?.id;
            if (!string.IsNullOrEmpty(storeId))
            {
                return storeId;
            }

            List<IPurchasedProductInfo> purchasedProducts = order?.Info?.PurchasedProductInfo;
            return purchasedProducts != null && purchasedProducts.Count > 0
                    ? purchasedProducts[0]?.productId ?? string.Empty
                    : string.Empty;
        }

        private static string GetOrderKey(Order order) => order?.Info?.TransactionID ?? string.Empty;

        private static void Raise(Action<IPurchaseResponse> handler, IPurchaseResponse response)
        {
            if (handler == null)
            {
                return;
            }

            foreach (Delegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    ((Action<IPurchaseResponse>)subscriber).Invoke(response);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void LogPurchaseFailure(PurchaseResponse response, string transactionId)
        {
            PurchaseFailureReason reason = response.FailureReason ?? PurchaseFailureReason.Unknown;
            string transactionReference = OrderReceiptReader.GetLogReference(transactionId);
            if (reason == PurchaseFailureReason.UserCancelled)
            {
                Debug.Log($"[{nameof(InAppPurchaseService)}] Purchase {response.ProductId} cancelled by user.");
                return;
            }

            Debug.LogException(new PurchaseFailedException(response.ProductId, reason, response.ErrorMessage, transactionReference));
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            _storeService.OnStoreConnected += StoreConnectedHandler;
            _storeService.OnStoreDisconnected += StoreDisconnectedHandler;
            _productService.OnProductsFetched += ProductsFetchedHandler;
            _productService.OnProductsFetchFailed += ProductsFetchFailedHandler;
            _purchaseService.OnPurchasesFetched += PurchasesFetchedHandler;
            _purchaseService.OnPurchasesFetchFailed += PurchasesFetchFailedHandler;
            _purchaseService.OnPurchasePending += PurchasePendingHandler;
            _purchaseService.OnPurchaseConfirmed += PurchaseConfirmedHandler;
            _purchaseService.OnPurchaseFailed += PurchaseFailedHandler;
            _purchaseService.OnPurchaseDeferred += PurchaseDeferredHandler;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _storeService.OnStoreConnected -= StoreConnectedHandler;
            _storeService.OnStoreDisconnected -= StoreDisconnectedHandler;
            _productService.OnProductsFetched -= ProductsFetchedHandler;
            _productService.OnProductsFetchFailed -= ProductsFetchFailedHandler;
            _purchaseService.OnPurchasesFetched -= PurchasesFetchedHandler;
            _purchaseService.OnPurchasesFetchFailed -= PurchasesFetchFailedHandler;
            _purchaseService.OnPurchasePending -= PurchasePendingHandler;
            _purchaseService.OnPurchaseConfirmed -= PurchaseConfirmedHandler;
            _purchaseService.OnPurchaseFailed -= PurchaseFailedHandler;
            _purchaseService.OnPurchaseDeferred -= PurchaseDeferredHandler;
            _isSubscribed = false;
        }

        private IProductInfo FindProductInfo(string productId)
        {
            for (int i = 0; i < _productCollection.Count; i++)
            {
                IProductInfo productInfo = _productCollection[i];
                if (productInfo.Id == productId)
                {
                    return productInfo;
                }
            }

            return null;
        }

        private Product GetStoreProduct(IProductInfo productInfo)
        {
            return string.IsNullOrEmpty(productInfo?.StoreId) || _productService == null
                    ? null
                    : _productService.GetProductById(productInfo.StoreId);
        }

        private void TryProcessNextPurchase()
        {
            if (_isPurchaseProcessing || !IsInitialized)
            {
                return;
            }

            if (_purchaseQueue.TryDequeue(out PurchaseQueueItem item))
            {
                _currentItem = item;
                _isPurchaseProcessing = true;
                _purchaseService.PurchaseProduct(item.Product);
            }
        }

        private bool IsCurrentPurchase(string storeId)
        {
            return _isPurchaseProcessing && (string.IsNullOrEmpty(storeId) || storeId == _currentItem.Info.StoreId);
        }

        private PurchaseResponse CreateResponse(IProductInfo productInfo, Order order, Product product)
        {
            CartItem cartItem = GetCartItem(order);
            return new PurchaseResponse
            {
                Product = cartItem?.Product ?? product ?? GetStoreProduct(productInfo),
                ProductId = productInfo.Id,
                TransactionId = OrderReceiptReader.GetTransactionId(order),
                Receipt = order?.Info?.Receipt ?? string.Empty,
                SignedTransaction = OrderReceiptReader.GetSignedTransaction(order),
                Quantity = OrderReceiptReader.GetQuantity(order, cartItem),
            };
        }

        private PurchaseResponse CreateCurrentFailure(PurchaseFailureReason reason, string errorMessage)
        {
            return new PurchaseResponse
            {
                Product = _currentItem.Product,
                ProductId = _currentItem.Info.Id,
                Status = PurchaseStatus.Failure,
                FailureReason = reason,
                ErrorMessage = errorMessage,
                IsAutoConfirm = _currentItem.AutoConfirm,
            };
        }

        private void CompleteCurrentPurchase(PurchaseResponse response, Action<IPurchaseResponse> handler)
        {
            _currentItem = default;
            _isPurchaseProcessing = false;
            Raise(handler, response);
            TryProcessNextPurchase();
        }

        private void FailCurrentPurchase(PurchaseFailureReason reason, string errorMessage)
        {
            PurchaseResponse response = CreateCurrentFailure(reason, errorMessage);
            LogPurchaseFailure(response, string.Empty);
            CompleteCurrentPurchase(response, OnPurchaseFailed);
        }

        private void FailAllPurchases(PurchaseFailureReason reason, string errorMessage)
        {
            if (_isPurchaseProcessing)
            {
                PurchaseResponse response = CreateCurrentFailure(reason, errorMessage);
                _currentItem = default;
                _isPurchaseProcessing = false;
                LogPurchaseFailure(response, string.Empty);
                Raise(OnPurchaseFailed, response);
            }

            while (_purchaseQueue.TryDequeue(out PurchaseQueueItem item))
            {
                var response = new PurchaseResponse
                {
                    Product = item.Product,
                    ProductId = item.Info.Id,
                    Status = PurchaseStatus.Failure,
                    FailureReason = reason,
                    ErrorMessage = errorMessage,
                    IsAutoConfirm = item.AutoConfirm,
                };

                LogPurchaseFailure(response, string.Empty);
                Raise(OnPurchaseFailed, response);
            }
        }

        private bool TryCreatePendingResponse(PendingOrder order, bool autoConfirm, bool isRestored, out PurchaseResponse response)
        {
            string storeId = GetStoreId(order);
            if (!_productsByStoreId.TryGetValue(storeId, out IProductInfo productInfo))
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Pending order for unconfigured product '{storeId}' is skipped.");
                response = null;
                return false;
            }

            response = CreateResponse(productInfo, order, null);
            response.Status = PurchaseStatus.Success;
            response.IsAutoConfirm = autoConfirm;
            response.IsRestored = isRestored;
            response.Order = order;
            return true;
        }

        private void DeliverRestoredPurchase(PendingOrder order)
        {
            string key = GetOrderKey(order);
            if (_pendingResponses.ContainsKey(key) || _confirmingResponses.ContainsKey(key))
            {
                return;
            }

            if (!TryCreatePendingResponse(order, false, true, out PurchaseResponse response))
            {
                return;
            }

            _pendingResponses[key] = response;
            Debug.Log($"[{nameof(InAppPurchaseService)}] Restored pending purchase {response.ProductId}, transactionRef={OrderReceiptReader.GetLogReference(key)}.");
            Raise(OnPurchased, response);
        }

        private void CacheSubscriptions(Order order)
        {
            List<IPurchasedProductInfo> purchasedProducts = order?.Info?.PurchasedProductInfo;
            if (purchasedProducts == null)
            {
                return;
            }

            for (int i = 0; i < purchasedProducts.Count; i++)
            {
                IPurchasedProductInfo purchasedProduct = purchasedProducts[i];
                if (purchasedProduct?.subscriptionInfo != null &&
                    !string.IsNullOrEmpty(purchasedProduct.productId) &&
                    _productsByStoreId.ContainsKey(purchasedProduct.productId))
                {
                    _subscriptions[purchasedProduct.productId] = purchasedProduct.subscriptionInfo;
                }
            }
        }

        private void FailInitialization(string message)
        {
            _initializationStage = InitializationStage.None;
            IsInitialized = false;
            var exception = new InitializationFailureException(message);
            LogStoreFailure(exception.Message);
            OnInitializeFailed?.Invoke(exception);
        }

        private void LogStoreFailure(string message)
        {
            bool isExpected = _disconnectMessage != null || Application.internetReachability == NetworkReachability.NotReachable;
            if (isExpected)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] {message}");
            }
            else
            {
                Debug.LogError($"[{nameof(InAppPurchaseService)}] {message}");
            }
        }

        private void RestorePurchasesCallback(bool result, string errorMessage)
        {
            if (!_isRestoreProcessing)
            {
                return;
            }

            _isRestoreProcessing = false;
            if (result)
            {
                Debug.Log($"[{nameof(InAppPurchaseService)}] Restoring successful!");
            }
            else
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Restoring failed! Message: {errorMessage}");
            }

            OnPurchasesRestored?.Invoke(result);
        }

        private void StoreConnectedHandler()
        {
            _disconnectMessage = null;
            if (_initializationStage != InitializationStage.Connecting)
            {
                return;
            }

            var definitions = new List<ProductDefinition>(_productsByStoreId.Count);
            foreach (IProductInfo productInfo in _productsByStoreId.Values)
            {
                definitions.Add(new ProductDefinition(productInfo.StoreId, productInfo.Type));
            }

            _initializationStage = InitializationStage.FetchingProducts;
            _productService.FetchProducts(definitions);
        }

        private void StoreDisconnectedHandler(StoreConnectionFailureDescription description)
        {
            string message = $"Store disconnected: {description?.Message}";
            _disconnectMessage = message;
            if (_initializationStage != InitializationStage.None)
            {
                FailInitialization(message);
                return;
            }

            if (!IsInitialized)
            {
                return;
            }

            IsInitialized = false;
            FailAllPurchases(PurchaseFailureReason.StoreNotConnected, message);
            if (_isRestoreProcessing)
            {
                _isRestoreProcessing = false;
                OnPurchasesRestored?.Invoke(false);
            }

            FailInitialization(message);
        }

        private void ProductsFetchedHandler(List<Product> products)
        {
            if (_initializationStage != InitializationStage.FetchingProducts)
            {
                return;
            }

            var fetchedStoreIds = new HashSet<string>();
            if (products != null)
            {
                for (int i = 0; i < products.Count; i++)
                {
                    string storeId = products[i]?.definition?.id;
                    if (!string.IsNullOrEmpty(storeId))
                    {
                        fetchedStoreIds.Add(storeId);
                    }
                }
            }

            if (fetchedStoreIds.Count == 0)
            {
                FailInitialization($"Store returned no products for {_productsByStoreId.Count} requested ones.");
                return;
            }

            var unresolvedStoreIds = new List<string>();
            foreach (string storeId in _productsByStoreId.Keys)
            {
                if (!fetchedStoreIds.Contains(storeId))
                {
                    unresolvedStoreIds.Add(storeId);
                }
            }

            if (unresolvedStoreIds.Count > 0)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Store returned no metadata for: {string.Join(", ", unresolvedStoreIds)}");
            }

            _initializationStage = InitializationStage.FetchingPurchases;
            _purchaseService.FetchPurchases();
        }

        private void ProductsFetchFailedHandler(ProductFetchFailed failure)
        {
            if (_initializationStage == InitializationStage.FetchingProducts)
            {
                FailInitialization($"Fetch products failed: {failure?.FailureReason}");
            }
        }

        private void PurchasesFetchedHandler(Orders orders)
        {
            if (orders == null)
            {
                return;
            }

            for (int i = 0; i < orders.ConfirmedOrders.Count; i++)
            {
                CacheSubscriptions(orders.ConfirmedOrders[i]);
            }

            for (int i = 0; i < orders.PendingOrders.Count; i++)
            {
                CacheSubscriptions(orders.PendingOrders[i]);
            }

            bool isInitializing = _initializationStage == InitializationStage.FetchingPurchases;
            if (isInitializing)
            {
                _initializationStage = InitializationStage.None;
                IsInitialized = true;
                Debug.Log($"[{nameof(InAppPurchaseService)}] Purchasing was been initialized!");
                OnInitialized?.Invoke();
            }

            if (!IsInitialized)
            {
                return;
            }

            for (int i = 0; i < orders.PendingOrders.Count; i++)
            {
                PendingOrder order = orders.PendingOrders[i];
                if (isInitializing && _confirmingResponses.ContainsKey(GetOrderKey(order)))
                {
                    _purchaseService.ConfirmPurchase(order);
                    continue;
                }

                DeliverRestoredPurchase(order);
            }
        }

        private void PurchasesFetchFailedHandler(PurchasesFetchFailureDescription description)
        {
            string message = $"Fetch purchases failed: {description?.Message}";
            if (_initializationStage == InitializationStage.FetchingPurchases)
            {
                FailInitialization(message);
                return;
            }

            LogStoreFailure(message);
        }

        private void PurchasePendingHandler(PendingOrder order)
        {
            CacheSubscriptions(order);
            if (IsCurrentPurchase(GetStoreId(order)))
            {
                if (!TryCreatePendingResponse(order, _currentItem.AutoConfirm, false, out PurchaseResponse response))
                {
                    FailCurrentPurchase(PurchaseFailureReason.Unknown, "Pending order could not be resolved to a configured product.");
                    return;
                }

                _pendingResponses[GetOrderKey(order)] = response;
                if (response.IsAutoConfirm)
                {
                    ConfirmPendingPurchase(response);
                }

                CompleteCurrentPurchase(response, OnPurchased);
                return;
            }

            if (IsInitialized)
            {
                DeliverRestoredPurchase(order);
            }
        }

        private void PurchaseConfirmedHandler(Order order)
        {
            string key = GetOrderKey(order);
            if (!_confirmingResponses.TryGetValue(key, out PurchaseResponse pendingResponse))
            {
                if (order is ConfirmedOrder)
                {
                    CacheSubscriptions(order);
                }

                return;
            }

            _confirmingResponses.Remove(key);
            var response = new PurchaseResponse
            {
                Product = pendingResponse.Product,
                ProductId = pendingResponse.ProductId,
                TransactionId = pendingResponse.TransactionId,
                Receipt = pendingResponse.Receipt,
                SignedTransaction = pendingResponse.SignedTransaction,
                Quantity = pendingResponse.Quantity,
                IsAutoConfirm = pendingResponse.IsAutoConfirm,
                IsRestored = pendingResponse.IsRestored,
            };

            if (order is ConfirmedOrder)
            {
                CacheSubscriptions(order);
                response.Status = PurchaseStatus.Success;
            }
            else
            {
                FailedOrder failedOrder = order as FailedOrder;
                response.Status = PurchaseStatus.Failure;
                response.FailureReason = failedOrder?.FailureReason ?? PurchaseFailureReason.Unknown;
                response.ErrorMessage = failedOrder?.Details ?? $"Unexpected confirmation order: {order?.GetType().Name}";
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Confirm {response.ProductId} failed: reason={response.FailureReason}, transactionRef={OrderReceiptReader.GetLogReference(key)}");
            }

            Raise(OnPurchaseConfirmed, response);
            if (IsInitialized)
            {
                _purchaseService.FetchPurchases();
            }
        }

        private void PurchaseFailedHandler(FailedOrder order)
        {
            if (order == null)
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase failed without order data.");
                return;
            }

            string storeId = GetStoreId(order);
            bool isCurrent = IsCurrentPurchase(storeId);
            IProductInfo productInfo;
            if (isCurrent)
            {
                productInfo = _currentItem.Info;
            }
            else if (!_productsByStoreId.TryGetValue(storeId, out productInfo))
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Purchase failed for unconfigured product '{storeId}': reason={order.FailureReason}");
                return;
            }

            PurchaseResponse response = CreateResponse(productInfo, order, isCurrent ? _currentItem.Product : null);
            response.Status = PurchaseStatus.Failure;
            response.FailureReason = order.FailureReason;
            response.ErrorMessage = order.Details ?? string.Empty;
            response.IsAutoConfirm = isCurrent && _currentItem.AutoConfirm;
            LogPurchaseFailure(response, GetOrderKey(order));
            if (isCurrent)
            {
                CompleteCurrentPurchase(response, OnPurchaseFailed);
                return;
            }

            Raise(OnPurchaseFailed, response);
        }

        private void PurchaseDeferredHandler(DeferredOrder order)
        {
            string storeId = GetStoreId(order);
            bool isCurrent = IsCurrentPurchase(storeId);
            IProductInfo productInfo;
            if (isCurrent)
            {
                productInfo = _currentItem.Info;
            }
            else if (!_productsByStoreId.TryGetValue(storeId, out productInfo))
            {
                Debug.LogWarning($"[{nameof(InAppPurchaseService)}] Deferred order for unconfigured product '{storeId}' is skipped.");
                return;
            }

            PurchaseResponse response = CreateResponse(productInfo, order, isCurrent ? _currentItem.Product : null);
            response.Status = PurchaseStatus.Deferred;
            response.IsAutoConfirm = isCurrent && _currentItem.AutoConfirm;
            Debug.Log($"[{nameof(InAppPurchaseService)}] Purchase {response.ProductId} is deferred.");
            if (isCurrent)
            {
                CompleteCurrentPurchase(response, OnPurchaseDeferred);
                return;
            }

            Raise(OnPurchaseDeferred, response);
        }
    }
}