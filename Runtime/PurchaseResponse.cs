using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    internal sealed class PurchaseResponse : IPurchaseResponse
    {
        public Product Product { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
        public string SignedTransaction { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public PurchaseStatus Status { get; set; }
        public PurchaseFailureReason? FailureReason { get; set; }
        public bool IsAutoConfirm { get; set; }
        public bool IsRestored { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        internal PendingOrder Order { get; set; }
    }
}