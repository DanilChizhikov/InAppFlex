namespace MbsCore.InAppFlex.Infrastructure
{
    public interface IPurchaseResponse
    {
        string ProductId { get; }
        string TransactionId { get; }
        string Receipt { get; }
        PurchaseStatus Status { get; }
        string ErrorMessage { get; }
    }
}