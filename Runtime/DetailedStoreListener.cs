using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace DTech.InAppFlex
{
	internal sealed class DetailedStoreListener : IDetailedStoreListener
	{
		public event Action<IStoreController, IExtensionProvider> OnInitialized;
		public event Action<InitializationFailureException> OnInitializeFailed;
		public event Action<PurchaseFailedException> OnPurchaseFailed;
		
		private readonly Func<PurchaseEventArgs, PurchaseProcessingResult> _processPurchase;

		public DetailedStoreListener(Func<PurchaseEventArgs, PurchaseProcessingResult> processPurchase)
		{
			_processPurchase = processPurchase;
		}
		
		void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			OnInitialized?.Invoke(controller, extensions);
		}
		
		void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
		{
			var exception = new InitializationFailureException(error);
			Debug.LogException(exception);
			OnInitializeFailed?.Invoke(exception);
		}

		void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
		{
			var exception = new InitializationFailureException(error, message);
			Debug.LogException(exception);
			OnInitializeFailed?.Invoke(exception);
		}

		PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
		{
			return _processPurchase.Invoke(purchaseEvent);
		}

		void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			var exception = new PurchaseFailedException(product, failureReason);
			Debug.LogException(exception);
			OnPurchaseFailed?.Invoke(exception);
		}

		void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
		{
			var exception = new PurchaseFailedException(product, failureDescription.reason, failureDescription.message);
			Debug.LogException(exception);
			OnPurchaseFailed?.Invoke(exception);
		}
	}
}