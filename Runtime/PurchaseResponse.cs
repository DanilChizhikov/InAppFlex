using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    internal sealed class PurchaseResponse : IPurchaseResponse
    {
        public Product Product { get; set; }
        public string TransactionId { get; set; }
        public string Receipt { get; set; }
        public PurchaseStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public bool CanConfirm { get; set; }

        public IPurchaseResponse Clone() => new PurchaseResponse
                {
                        Product = Product,
                        TransactionId = TransactionId,
                        Receipt = Receipt,
                        Status = Status,
                        ErrorMessage = ErrorMessage,
                        CanConfirm = CanConfirm,
                };
    }
}