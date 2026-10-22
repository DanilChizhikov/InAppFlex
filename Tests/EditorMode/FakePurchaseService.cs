using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class FakePurchaseService : IPurchaseService
    {
        public event Action<PendingOrder> OnPurchasePending;
        public event Action<Order> OnPurchaseConfirmed;
        public event Action<FailedOrder> OnPurchaseFailed;
        public event Action<DeferredOrder> OnPurchaseDeferred;
        public event Action<Orders> OnPurchasesFetched;
        public event Action<PurchasesFetchFailureDescription> OnPurchasesFetchFailed;
        public event Action<Entitlement> OnCheckEntitlement { add { } remove { } }

        public IAppleStoreExtendedPurchaseService Apple => null;
        public IGooglePlayStoreExtendedPurchaseService Google => null;
        public List<Product> PurchasedProducts { get; } = new();
        public List<PendingOrder> ConfirmedOrders { get; } = new();
        public Orders FetchedOrders { get; set; } = StoreFixtures.CreateOrders();
        public bool? ProcessPendingOrdersOnFetch { get; private set; }
        public int FetchCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public int PendingSubscribers => OnPurchasePending?.GetInvocationList().Length ?? 0;

        public void PurchaseProduct(Product product)
        {
            PurchasedProducts.Add(product);
        }

        public void Purchase(ICart cart)
        {
        }

        public void ConfirmPurchase(PendingOrder order)
        {
            ConfirmedOrders.Add(order);
        }

        public void FetchPurchases()
        {
            FetchCalls++;
            if (FetchedOrders != null)
            {
                OnPurchasesFetched?.Invoke(FetchedOrders);
            }
        }

        public void CheckEntitlement(Product product)
        {
        }

        public void RestoreTransactions(Action<bool, string> callback)
        {
            RestoreCalls++;
            callback?.Invoke(true, null);
        }

        public ReadOnlyObservableCollection<Order> GetPurchases()
        {
            return new ReadOnlyObservableCollection<Order>(new ObservableCollection<Order>());
        }

        public void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)
        {
            ProcessPendingOrdersOnFetch = shouldProcess;
        }

        public void RaisePending(PendingOrder order)
        {
            OnPurchasePending?.Invoke(order);
        }

        public void RaiseConfirmed(Order order)
        {
            OnPurchaseConfirmed?.Invoke(order);
        }

        public void RaiseFailed(FailedOrder order)
        {
            OnPurchaseFailed?.Invoke(order);
        }

        public void RaiseDeferred(DeferredOrder order)
        {
            OnPurchaseDeferred?.Invoke(order);
        }

        public void RaiseFetchFailed(string message)
        {
            OnPurchasesFetchFailed?.Invoke(new PurchasesFetchFailureDescription(PurchasesFetchFailureReason.Unknown, message));
        }
    }
}