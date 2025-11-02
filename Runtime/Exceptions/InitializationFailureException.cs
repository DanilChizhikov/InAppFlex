using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class InitializationFailureException : Exception
	{
		internal InitializationFailureException(InitializationFailureReason reason) : base($"{nameof(IStoreController)} initialization failed: {reason}")
		{
		}
		
		internal InitializationFailureException(InitializationFailureReason reason, string message) : base($"{nameof(IStoreController)} initialization failed: {reason}, message: {message}")
		{
		}
	}
}