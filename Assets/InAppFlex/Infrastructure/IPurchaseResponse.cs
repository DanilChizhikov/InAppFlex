using UnityEngine.Purchasing;

namespace MbsCore.InAppFlex.Infrastructure
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