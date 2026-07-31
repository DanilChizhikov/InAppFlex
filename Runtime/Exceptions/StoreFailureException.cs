using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class StoreFailureException : Exception
	{
		public StoreFailureStage Stage { get; }
		public bool IsRetryable { get; }

		internal StoreFailureException(StoreConnectionFailureDescription description) :
			base($"{nameof(StoreController)} failed on {StoreFailureStage.StoreConnection}, message: {description.Message}")
		{
			Stage = StoreFailureStage.StoreConnection;
			IsRetryable = description.IsRetryable;
		}

		internal StoreFailureException(ProductFetchFailed failure) :
			base($"{nameof(StoreController)} failed on {StoreFailureStage.ProductsFetch}, products: {failure.FailedFetchProducts.Count}, reason: {failure.FailureReason}")
		{
			Stage = StoreFailureStage.ProductsFetch;
			IsRetryable = true;
		}

		internal StoreFailureException(PurchasesFetchFailureDescription description) :
			base($"{nameof(StoreController)} failed on {StoreFailureStage.PurchasesFetch}, reason: {description.FailureReason}, message: {description.Message}")
		{
			Stage = StoreFailureStage.PurchasesFetch;
			IsRetryable = description.FailureReason != PurchasesFetchFailureReason.PurchasingUnavailable;
		}

		internal StoreFailureException(string restoreErrorMessage) :
			base($"{nameof(StoreController)} failed on {StoreFailureStage.PurchasesRestore}, message: {restoreErrorMessage}")
		{
			Stage = StoreFailureStage.PurchasesRestore;
			IsRetryable = true;
		}
	}
}
