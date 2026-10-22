using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public sealed class InitializationFailureException : Exception
	{
		internal InitializationFailureException(string message) : base($"{nameof(StoreController)} initialization failed: {message}")
		{
		}
	}
}