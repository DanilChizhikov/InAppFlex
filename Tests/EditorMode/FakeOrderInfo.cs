using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class FakeOrderInfo : IOrderInfo, IAppleOrderInfo
    {
        public IAppleOrderInfo Apple => string.IsNullOrEmpty(jwsRepresentation) ? null : this;
        public IGoogleOrderInfo Google => null;
        public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; } = new();
        public string Receipt { get; }
        public string TransactionID { get; }
        public string AppReceipt => string.Empty;
        public string OriginalTransactionID { get; set; }
        public OwnershipType OwnershipType { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public Guid? AppAccountToken { get; set; }
        public string jwsRepresentation { get; set; }

        public FakeOrderInfo(string transactionId, string receipt = "")
        {
            TransactionID = transactionId;
            Receipt = receipt;
        }
    }
}