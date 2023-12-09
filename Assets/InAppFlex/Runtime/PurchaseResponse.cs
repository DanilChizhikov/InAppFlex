using MbsCore.InAppFlex.Infrastructure;

namespace MbsCore.InAppFlex.Runtime
{
    internal sealed class PurchaseResponse : IPurchaseResponse
    {
        public string ProductId { get; set; }
        public string TransactionId { get; set; }
        public string Receipt { get; set; }
        public PurchaseStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}