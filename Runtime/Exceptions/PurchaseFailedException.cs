using System;
using System.Linq;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class PurchaseFailedException : Exception
	{
		public string ProductId { get; }
		public PurchaseFailureReason Reason { get; }
		public string ErrorMessage { get; }

		internal Product Product { get; }
		internal FailedOrder Order { get; }

		internal PurchaseFailedException(FailedOrder order) : base(GetMessage(order))
		{
			Order = order;
			Product = order.CartOrdered?.Items()?.FirstOrDefault()?.Product;
			ProductId = Product == null ? string.Empty : Product.definition.id;
			Reason = order.FailureReason;
			ErrorMessage = string.IsNullOrEmpty(order.Details) ? GetMessage(order) : order.Details;
		}

		private static string GetMessage(FailedOrder order)
		{
			Product product = order.CartOrdered?.Items()?.FirstOrDefault()?.Product;
			string productId = product == null ? "Unknown" : product.definition.id;
			return $"Product: {productId}, PurchaseFailureReason: {order.FailureReason}, Details: {order.Details}";
		}
	}
}