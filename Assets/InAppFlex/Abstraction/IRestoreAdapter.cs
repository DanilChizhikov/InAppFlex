using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Abstraction
{
    public interface IRestoreAdapter
    {
        bool IsAvailable { get; }
        
        void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback);
    }
}