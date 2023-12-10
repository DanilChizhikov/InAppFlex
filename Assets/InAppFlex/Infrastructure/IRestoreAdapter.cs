using System;
using UnityEngine.Purchasing;

namespace MbsCore.InAppFlex.Infrastructure
{
    public interface IRestoreAdapter
    {
        bool IsAvailable { get; }
        
        void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback);
    }
}