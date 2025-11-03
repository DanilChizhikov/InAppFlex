using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IPurchaseResponse
    {
        Product Product { get; }
        string TransactionId { get; }
        string Receipt { get; }
        PurchaseStatus Status { get; }
        bool IsAutoConfirm { get; }
        string ErrorMessage { get; }
    }
}