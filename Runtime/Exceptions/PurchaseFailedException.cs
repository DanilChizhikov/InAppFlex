using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class PurchaseFailedException : Exception
	{
		public string ProductId { get; }
		public PurchaseFailureReason Reason { get; }
		public string ErrorMessage { get; }

		internal PurchaseFailedException(string productId, PurchaseFailureReason reason, string errorMessage, string transactionReference) :
			base($"Product: {productId}, PurchaseFailureReason: {reason}, TransactionRef: {transactionReference}")
		{
			ProductId = productId;
			Reason = reason;
			ErrorMessage = errorMessage;
		}
	}
}