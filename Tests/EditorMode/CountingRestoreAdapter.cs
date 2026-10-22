using System;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class CountingRestoreAdapter : IRestoreAdapter
    {
        public bool IsAvailable => true;
        public int Calls { get; private set; }

        public void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback)
        {
            Calls++;
            callback?.Invoke(true, string.Empty);
        }
    }
}