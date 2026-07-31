using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class InitializationFailureException : Exception
	{
		public InitializationFailureStage Stage { get; }
		public bool IsRetryable { get; }

		internal InitializationFailureException(StoreConnectionFailureDescription description) :
			base($"{nameof(StoreController)} initialization failed on {InitializationFailureStage.StoreConnection}, message: {description.Message}")
		{
			Stage = InitializationFailureStage.StoreConnection;
			IsRetryable = description.IsRetryable;
		}

		internal InitializationFailureException(ProductFetchFailed failure) :
			base($"{nameof(StoreController)} initialization failed on {InitializationFailureStage.ProductsFetch}, products: {failure.FailedFetchProducts.Count}, reason: {failure.FailureReason}")
		{
			Stage = InitializationFailureStage.ProductsFetch;
			IsRetryable = true;
		}

		internal InitializationFailureException(Exception innerException) :
			base($"{nameof(StoreController)} initialization failed on {InitializationFailureStage.StoreConnection}, message: {innerException.Message}", innerException)
		{
			Stage = InitializationFailureStage.StoreConnection;
			IsRetryable = true;
		}
	}
}