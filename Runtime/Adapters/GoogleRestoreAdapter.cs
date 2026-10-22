using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public sealed class GoogleRestoreAdapter : RestoreAdapter
    {
        public override bool IsAvailable => Application.platform == RuntimePlatform.Android;

        public override void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback)
        {
            purchaseService.RestoreTransactions(callback);
        }
    }
}