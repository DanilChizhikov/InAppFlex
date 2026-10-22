using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class FakePurchasedProductInfo : IPurchasedProductInfo
    {
        public string productId { get; }
        public SubscriptionInfo subscriptionInfo { get; }

        public FakePurchasedProductInfo(string productId, SubscriptionInfo subscriptionInfo)
        {
            this.productId = productId;
            this.subscriptionInfo = subscriptionInfo;
        }
    }
}