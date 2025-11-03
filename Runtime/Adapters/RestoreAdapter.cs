using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    public abstract class RestoreAdapter : IRestoreAdapter
    {
        public abstract bool IsAvailable { get; }

        public abstract void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback);
    }
}