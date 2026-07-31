using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    internal sealed class PurchaseResponse : IPurchaseResponse
    {
        public Product Product { get; }
        public string ProductId => Product == null ? string.Empty : Product.definition.id;
        public string TransactionId => Order == null ? string.Empty : Order.Info.TransactionID;
        public string Receipt => Order == null ? string.Empty : Order.Info.Receipt;
        public PurchaseStatus Status { get; set; }
        public bool IsAutoConfirm { get; set; }
        public string ErrorMessage { get; set; }

        internal Order Order { get; }
        internal PendingOrder PendingOrder => Order as PendingOrder;

        public PurchaseResponse(Order order, Product product)
        {
            Order = order;
            Product = product;
        }
    }
}
