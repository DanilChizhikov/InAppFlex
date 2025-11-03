using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IInAppPurchaseService
    {
        event Action OnInitialized;
        event Action<InitializationFailureException> OnInitializeFailed;
        event Action<IPurchaseResponse> OnPurchased;
        event Action<bool> OnPurchasesRestored;
        event Action<IPurchaseResponse> OnPurchaseFailed;

        bool IsInitialized { get; }
        
        void Initialize();
        void Purchase(string productId, bool autoConfirm = false);
        decimal GetPrice(string productId);
        string GetStringCurrency(string productId);
        void ConfirmPendingPurchase(IPurchaseResponse response);
        bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo);
        void RestorePurchases();
    }
}