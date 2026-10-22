using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public abstract class RestoreAdapter : IRestoreAdapter
    {
        public abstract bool IsAvailable { get; }

        public abstract void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback);
    }
}