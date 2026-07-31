# Changelog

## [3.0.0] - 2026-07-31

Migration to Unity IAP v5.

### Breaking
- Requires `com.unity.purchasing` 5.3.1+. The v4 API (`UnityPurchasing.Initialize`, `IStoreListener`, `ConfigurationBuilder`, `IExtensionProvider`, `SubscriptionManager`) is no longer used.
- `InAppPurchaseService` constructor no longer takes `IEnumerable<IRestoreAdapter>`.
- `IRestoreAdapter`, `RestoreAdapter`, `AppleRestoreAdapter` and `GoogleRestoreAdapter` removed. Restoring goes through the cross-store `StoreController.RestoreTransactions`.
- `InitializationFailureException` now exposes `Stage` and `IsRetryable` instead of the removed `InitializationFailureReason`.
- `PurchaseFailedException` is built from a `FailedOrder`, its `Product` based constructors are gone.
- The synchronous `Initialize`, `Purchase` and `RestorePurchases` are removed, use `InitializeAsync`, `PurchaseAsync` and `RestorePurchasesAsync`.
- Starting an operation that is already in flight throws an `InvalidOperationException`: `InitializeAsync`, `RestorePurchasesAsync`, and `PurchaseAsync` for a product that is already being purchased.

### Added
- `InitializeAsync`, `PurchaseAsync` and `RestorePurchasesAsync` as the only entry points, the events are still raised for observers.
- `IPurchaseResponse.ProductId`.
- `Dispose` completes pending operations instead of leaving their tasks hanging.

### Fixed
- Products were registered by `StoreId` but resolved by `Id`, so every product with different `Id` and `StoreId` was unpurchasable.
- Purchases left unconfirmed in a previous session are now reported through `OnPurchased` instead of being dropped.

## [2.0.0] - 2025-11-03

Initial release