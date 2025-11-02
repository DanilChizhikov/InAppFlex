using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IInAppPurchaseService
    {
        event Action OnInitialized;
        event Action OnInitializedFailed;
        event Action<bool> OnPurchasesRestored;
        event Action<IPurchaseResponse> OnPurchased;

        bool IsInitialized { get; }
        
        void Initialize(Dictionary<ProductType, HashSet<string>> products);
        void Purchase(string productId, bool autoConfirm = false);
        decimal GetPrice(string productId);
        string GetStringCurrency(string productId);
        void ConfirmPendingPurchase(IPurchaseResponse response);
        bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo);
        void RestorePurchases();
    }
}