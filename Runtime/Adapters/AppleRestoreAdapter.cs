using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public sealed class AppleRestoreAdapter : RestoreAdapter
    {
        public override bool IsAvailable => Application.platform == RuntimePlatform.tvOS ||
                                            Application.platform == RuntimePlatform.VisionOS ||
                                            Application.platform == RuntimePlatform.IPhonePlayer;

        public override void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback)
        {
            purchaseService.RestoreTransactions(callback);
        }
    }
}