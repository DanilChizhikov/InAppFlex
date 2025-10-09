using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Runtime.Adapters
{
    public sealed class GoogleRestoreAdapter : RestoreAdapter
    {
        public override bool IsAvailable => Application.platform == RuntimePlatform.Android;
        
        public override void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback)
        {
            var extension = provider.GetExtension<IGooglePlayStoreExtensions>();
            extension.RestoreTransactions(callback);
        }
    }
}