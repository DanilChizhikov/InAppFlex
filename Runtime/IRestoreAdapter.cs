using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public interface IRestoreAdapter
    {
        bool IsAvailable { get; }

        void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback);
    }
}