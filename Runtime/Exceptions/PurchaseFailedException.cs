using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class PurchaseFailedException : Exception
	{
		public string ProductId { get; }
		public PurchaseFailureReason Reason { get; }
		public string Message { get; }

		internal PurchaseFailedException(string productId, PurchaseFailureReason reason) :
			base($"Product: {productId}, PurchaseFailureReason: {reason}")
		{
			ProductId = productId;
			Reason = reason;
		}

		internal PurchaseFailedException(string productId, PurchaseFailureReason reason, string message) :
			base($"Product: {productId}, PurchaseFailureReason: {reason}, Message: {message}")
		{
			
		}
	}
}