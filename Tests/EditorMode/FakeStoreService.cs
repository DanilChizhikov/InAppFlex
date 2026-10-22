using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class FakeStoreService : IStoreService
    {
        public event Action<StoreConnectionFailureDescription> OnStoreDisconnected;
        public event Action OnStoreConnected;

        public IAppleStoreExtendedService Apple => null;
        public IGooglePlayStoreExtendedService Google => null;
        public bool ConnectSucceeds { get; set; } = true;
        public int ConnectCalls { get; private set; }
        public int ConnectedSubscribers => OnStoreConnected?.GetInvocationList().Length ?? 0;
        public int DisconnectedSubscribers => OnStoreDisconnected?.GetInvocationList().Length ?? 0;

        public Task Connect()
        {
            ConnectCalls++;
            if (ConnectSucceeds)
            {
                RaiseConnected();
            }
            else
            {
                RaiseDisconnected("no store");
            }

            return Task.CompletedTask;
        }

        public void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy retryPolicy)
        {
        }

        public void RaiseConnected()
        {
            OnStoreConnected?.Invoke();
        }

        public void RaiseDisconnected(string message)
        {
            OnStoreDisconnected?.Invoke(new StoreConnectionFailureDescription(message));
        }
    }
}