using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace MbsCore.InAppFlex.Infrastructure
{
    public interface IInAppService : IDisposable
    {
        event Action<IPurchaseResponse> OnPurchased;

        bool IsInitialized { get; }
        
        void Initialize(Dictionary<ProductType, HashSet<string>> products);
        void Purchase(string productId);
        decimal GetPrice(string productId);
        string GetStringCurrency(string productId);
        void ConfirmPendingPurchase(IPurchaseResponse response);
        bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo);
        void RestorePurchases();
    }
}