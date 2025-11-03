using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    internal sealed class PurchaseResponse : IPurchaseResponse
    {
        public Product Product { get; }
        public string TransactionId => Product.transactionID;
        public string Receipt => Product.receipt;
        public PurchaseStatus Status { get; set; }
        public bool IsAutoConfirm { get; set; }
        public string ErrorMessage { get; set; }

        public PurchaseResponse(Product product)
        {
            Product = product;
        }
    }
}