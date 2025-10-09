using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Abstraction
{
    public interface IPurchaseResponse
    {
        Product Product { get; }
        string TransactionId { get; }
        string Receipt { get; }
        PurchaseStatus Status { get; }
        string ErrorMessage { get; }
    }
}