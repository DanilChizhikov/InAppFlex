using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IPurchaseResponse
    {
        Product Product { get; }
        string ProductId { get; }
        string TransactionId { get; }
        string Receipt { get; }
        string SignedTransaction { get; }
        int Quantity { get; }
        PurchaseStatus Status { get; }
        PurchaseFailureReason? FailureReason { get; }
        bool IsAutoConfirm { get; }
        bool IsRestored { get; }
        string ErrorMessage { get; }
    }
}