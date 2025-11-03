using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class PurchaseFailedException : Exception
	{
		public string ProductId => Product.definition.id;
		public PurchaseFailureReason Reason { get; }
		public string ErrorMessage { get; }
		
		internal Product Product { get; }

		internal PurchaseFailedException(Product product, PurchaseFailureReason reason) :
			base($"Product: {product.definition.id}, PurchaseFailureReason: {reason}")
		{
			Product = product;
			Reason = reason;
			ErrorMessage = $"Product: {product.definition.id}, PurchaseFailureReason: {reason}";
		}

		internal PurchaseFailedException(Product product, PurchaseFailureReason reason, string message) :
			base($"Product: {product.definition.id}, PurchaseFailureReason: {reason}, Message: {message}")
		{
			Product = product;
			Reason = reason;
			ErrorMessage = message;
		}
	}
}