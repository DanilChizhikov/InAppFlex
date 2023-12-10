using System;
using MbsCore.InAppFlex.Infrastructure;
using UnityEngine.Purchasing;

namespace MbsCore.InAppFlex.Runtime
{
    public abstract class RestoreAdapter : IRestoreAdapter
    {
        public abstract bool IsAvailable { get; }

        public abstract void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback);
    }
}