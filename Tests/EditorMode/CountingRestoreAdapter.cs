using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class CountingRestoreAdapter : IRestoreAdapter
    {
        public bool IsAvailable { get; set; } = true;
        public int Calls { get; private set; }
        public IPurchaseService PurchaseService { get; private set; }

        public void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback)
        {
            Calls++;
            PurchaseService = purchaseService;
            callback?.Invoke(true, string.Empty);
        }
    }
}