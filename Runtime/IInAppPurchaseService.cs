using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IInAppPurchaseService
    {
        event Action OnInitialized;
        event Action<InitializationFailureException> OnInitializeFailed;
        event Action<StoreFailureException> OnStoreFailed;
        event Action<IPurchaseResponse> OnPurchased;
        event Action<bool> OnPurchasesRestored;
        event Action<IPurchaseResponse> OnPurchaseFailed;
        event Action<IPurchaseResponse> OnPurchaseDeferred;

        bool IsInitialized { get; }
        IReadOnlyCollection<string> DeferredProductIds { get; }

        Task<bool> InitializeAsync(CancellationToken token = default);
        Task<IPurchaseResponse> PurchaseAsync(string productId, bool autoConfirm = false, CancellationToken token = default);
        decimal GetPrice(string productId);
        string GetStringCurrency(string productId);
        void ConfirmPendingPurchase(IPurchaseResponse response);
        bool IsPurchaseDeferred(string productId);
        bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo);
        Task<bool> RestorePurchasesAsync(CancellationToken token = default);
    }
}