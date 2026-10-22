# Changelog

## [Unreleased]

### Changed
- The minimum Unity version is now 6000.0.
- **Breaking:** requires Unity IAP 5.3.1. The service is built on `StoreController` instead of the deprecated
  `UnityPurchasing.Initialize` and `IDetailedStoreListener`.
- **Breaking:** `IRestoreAdapter.RestorePurchases` takes the Unity IAP `IPurchaseService` instead of
  `IExtensionProvider`. `AppleRestoreAdapter` and `GoogleRestoreAdapter` call `RestoreTransactions` on it.
- **Breaking:** `IProductInfo` has `DefaultPrice` and `DefaultCurrency`.
- **Behaviour:** a purchase without `autoConfirm` stays pending in the store until `ConfirmPendingPurchase` is called.
  Before, it was finished as soon as it arrived.
- `OnInitializeFailed` is also raised when the store returns none of the products, fails to fetch purchases or
  disconnects after initialization. A failed or lost connection is logged as a warning.
- A purchase cancelled by the player is logged as information instead of an exception, and a failed restore as a
  warning. Failure logs no longer contain transaction IDs or purchase tokens.

### Added
- EditMode tests for `ProductCollection`, `InAppPurchaseService` and the order receipt parsing, run on every push and
  pull request to `master` and `develop`.
- Releases: a pull request from `release/X.Y.Z` or `hotfix/X.Y.Z` into `master` is checked for the version, the
  changelog and the release notes, and once merged it publishes `vX.Y.Z` with a `.unitypackage` attached.
- Purchases paid but not confirmed in an earlier session, approved later or delivered again by the store are raised
  through `OnPurchased` after `OnInitialized` with `IsRestored == true` and `IsAutoConfirm == false`, once per session.
- Deferred purchases (Ask to Buy, Google Play pending payments): `PurchaseStatus.Deferred` and `OnPurchaseDeferred`.
- `OnPurchaseConfirmed` with the result of `ConfirmPendingPurchase` and of automatic confirmation.
- `IPurchaseResponse.ProductId`, `SignedTransaction` (App Store JWS), `Quantity`, `FailureReason` and `IsRestored`.
  On Google Play `TransactionId` is the `GPA.` order ID when the receipt has one.
- Default price and currency per product in `ProductCollection`, returned while the store has no metadata.
- A confirmation lost to a disconnect is sent again after the next successful `Initialize()`.

### Fixed
- Purchases in the queue were never started after initialization.
- A purchase whose `Id` differs from its `StoreId` could not be bought and had no price.
- `GetPrice` and `GetStringCurrency` threw before initialization for a product from the collection.
- A failed or deferred purchase no longer blocks the next purchase in the queue, and a store disconnect fails the
  current and queued purchases instead of leaving them waiting.
- An order for a product missing from the collection is skipped with a warning and never granted.
- `RestorePurchases` raises `OnPurchasesRestored(false)` when no adapter is available instead of not answering.
- `Dispose` unsubscribes from the store even when initialization did not finish.

## [2.0.0] - 2025-11-03

Initial release