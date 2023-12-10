using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace MbsCore.InAppFlex.Runtime.Adapters
{
    public sealed class AppleRestoreAdapter : RestoreAdapter
    {
        public override bool IsAvailable => Application.platform == RuntimePlatform.tvOS ||
                                            Application.platform == RuntimePlatform.VisionOS ||
                                            Application.platform == RuntimePlatform.IPhonePlayer;
        
        public override void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback)
        {
            var extension = provider.GetExtension<IAppleExtensions>();
            extension.RestoreTransactions(callback);
        }
    }
}