# InAppFlex
[![Unity Version](https://img.shields.io/badge/unity-6000.0+-000.svg)](https://unity.com/releases/editor/archive)
![Unity Tests](https://github.com/DanilChizhikov/InAppFlex/actions/workflows/tests.yml/badge.svg?branch=master)
[![Donate](https://img.shields.io/badge/donate-DonationAlerts-f59c07.svg)](https://www.donationalerts.com/r/danilchizhikov)

## Overview
InAppFlex is a lightweight in-app purchase service for Unity built on top of
[Unity IAP 5](https://docs.unity3d.com/Manual/com.unity.purchasing.html). You describe your products in a
`ProductCollection` asset, create an `InAppPurchaseService`, and get events for initialization, purchases and
restoring. Purchases are queued and processed one at a time, pending purchases can be confirmed later, purchases left
unconfirmed in an earlier session are delivered again, and restoring is handled by pluggable adapters for the App Store
and Google Play.

## Table of Contents
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [UPM Installation](#upm-installation)
- [Features](#features)
- [Usage](#usage)
    - [Setting up Products](#setting-up-products)
    - [Initializing the Service](#initializing-the-service)
    - [Making a Purchase](#making-a-purchase)
    - [Unfinished and Deferred Purchases](#unfinished-and-deferred-purchases)
    - [Restoring Purchases](#restoring-purchases)
- [API Reference](#api-reference)
    - [IInAppPurchaseService](#iinapppurchaseservice)
    - [ProductInfo](#productinfo)
    - [ProductCollection](#productcollection)
    - [IPurchaseResponse](#ipurchaseresponse)
    - [IRestoreAdapter](#irestoreadapter)
- [Migrating from 2.x](#migrating-from-2x)
- [Support](#support)
- [License](#license)

## Getting Started

### Prerequisites
- [Unity](https://unity.com/releases/editor/archive) 6000.0+
- [Unity IAP](https://docs.unity3d.com/Manual/com.unity.purchasing.html) 5.3.1+

### UPM Installation
1. Open the manifest.json file in your project's Packages folder.
2. Add the following line to the dependencies section:
    ```json
    "com.dtech.inappflex": "https://github.com/DanilChizhikov/InAppFlex.git",
    ```
3. Unity will automatically import the package.

To pin a release, add its tag: `https://github.com/DanilChizhikov/InAppFlex.git#v2.0.0`.

InAppFlex depends on `com.unity.purchasing`; Unity installs it automatically. Every
[release](https://github.com/DanilChizhikov/InAppFlex/releases) also has a `.unitypackage` attached.

## Features
- 🛒 **Cross-Platform Support**: Works with both iOS and Android in-app purchases
- 🔄 **Purchase Restoration**: Built-in support for restoring purchases across devices
- 🔄 **Asynchronous Operations**: Non-blocking purchase flow with event-based callbacks
- 🏷️ **Product Management**: Easy management of in-app products with platform-specific store IDs
- 🧾 **Unfinished Purchases**: Purchases that were paid but not confirmed are delivered again on the next start
- ⏳ **Deferred Purchases**: Ask to Buy and Google Play pending payments are reported instead of blocking the queue
- 🔍 **Subscription Support**
- 💰 **Price Information**: Get localized prices and currency codes, with fallback values from the asset
- 🛡️ **Error Handling**: Failure reasons, confirmation results and data for server-side validation
- 🔌 **Extensible Architecture**: Support for custom restore adapters

## Usage

### Setting up Products
Create a `ProductCollection` asset and add your in-app products:

1. Right-click in the Project window → Create → DTech → InAppFlex → ProductCollection
2. Add your products with their respective store IDs for different platforms
3. Configure product types (Consumable, Non-Consumable, Subscription)
4. Optionally set a default price and currency. They are returned while the store has not loaded the product metadata

### Initializing the Service

```csharp
// Create product collection reference
[SerializeField] private ProductCollection _productCollection;

// Create restore adapters (optional)
private readonly List<IRestoreAdapter> _restoreAdapters = new()
{
    new AppleRestoreAdapter(),
    new GoogleRestoreAdapter()
};

// Initialize the service
private IInAppPurchaseService _purchaseService;

private void Awake()
{
    _purchaseService = new InAppPurchaseService(_productCollection, _restoreAdapters);
    _purchaseService.OnInitialized += OnInitialized;
    _purchaseService.OnInitializeFailed += OnInitializeFailed;
    _purchaseService.OnPurchased += OnPurchaseCompleted;
    _purchaseService.OnPurchaseFailed += OnPurchaseFailed;
    _purchaseService.OnPurchaseDeferred += OnPurchaseDeferred;
    _purchaseService.OnPurchaseConfirmed += OnPurchaseConfirmed;
    _purchaseService.OnPurchasesRestored += OnPurchasesRestored;
    
    _purchaseService.Initialize();
}
```

Initialization connects to the store, fetches the products from the collection and then the purchases the player
already owns. `OnInitializeFailed` is raised when the store cannot be reached, returns none of the products, or
disconnects later. After that `IsInitialized` is `false` and `Initialize()` can be called again.

### Making a Purchase

```csharp
public void PurchaseProduct(string productId)
{
    _purchaseService.Purchase(productId);
}

private void OnPurchaseCompleted(IPurchaseResponse response)
{
    Debug.Log($"Purchase successful: {response.ProductId}");
    
    // Grant the reward first, then confirm the purchase
    if (!response.IsAutoConfirm)
    {
        _purchaseService.ConfirmPendingPurchase(response);
    }
}

private void OnPurchaseFailed(IPurchaseResponse response)
{
    if (response.FailureReason != PurchaseFailureReason.UserCancelled)
    {
        Debug.LogWarning($"Purchase failed: {response.ProductId}, {response.ErrorMessage}");
    }
}
```

With `Purchase(productId, autoConfirm: true)` the service confirms the purchase itself before raising `OnPurchased`.
Without it the purchase stays pending in the store until `ConfirmPendingPurchase` is called, so a reward that was not
granted because the game closed is not lost. `OnPurchaseConfirmed` reports the result of every confirmation, including
a failed one.

### Unfinished and Deferred Purchases
A purchase can reach the game outside of a `Purchase` call: it was paid but not confirmed before the game closed, a
parent approved an Ask to Buy request, or the store delivered it again. Such purchases are raised through `OnPurchased`
after `OnInitialized` with `IsRestored == true` and `IsAutoConfirm == false`, so the code above grants and confirms them
as well. Each one is raised once per session.

When the payment needs approval or is still processing, `OnPurchaseDeferred` is raised with
`Status == PurchaseStatus.Deferred` and the queue moves on. Do not grant anything for it: once the payment goes
through, the purchase arrives through `OnPurchased` with `IsRestored == true`.

### Restoring Purchases

```csharp
public void RestorePurchases()
{
    _purchaseService.RestorePurchases();
}

private void OnPurchasesRestored(bool success)
{
    Debug.Log(success ? "Purchases restored successfully" : "Failed to restore purchases");
}
```

`OnPurchasesRestored(false)` is also raised when none of the adapters is available on the current platform or the store
disconnects while restoring.

## API Reference

### `IInAppPurchaseService`

#### Properties
- `bool IsInitialized` - Indicates if the service is ready to process purchases

#### Events
- `event Action OnInitialized` - Triggered when the service is successfully initialized
- `event Action<InitializationFailureException> OnInitializeFailed` - Triggered when initialization fails or the store disconnects
- `event Action<IPurchaseResponse> OnPurchased` - Triggered when a purchase is successful or an unfinished purchase is restored
- `event Action<bool> OnPurchasesRestored` - Triggered when restore purchases operation completes
- `event Action<IPurchaseResponse> OnPurchaseFailed` - Triggered when a purchase fails
- `event Action<IPurchaseResponse> OnPurchaseDeferred` - Triggered when a purchase waits for approval or payment
- `event Action<IPurchaseResponse> OnPurchaseConfirmed` - Triggered when the store confirms a purchase or fails to

#### Methods
- `void Initialize()` - Initializes the purchase service
- `void Purchase(string productId, bool autoConfirm = false)` - Initiates a purchase
- `decimal GetPrice(string productId)` - Gets the price of a product
- `string GetStringCurrency(string productId)` - Gets the currency code for a product
- `void ConfirmPendingPurchase(IPurchaseResponse response)` - Confirms a pending purchase
- `bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo)` - Gets subscription information
- `void RestorePurchases()` - Restores previous purchases
- `void Dispose()` - Cleans up resources

### `ProductInfo`
Represents an in-app product.

#### Properties
- `string Id` - The product's unique identifier
- `ProductType Type` - The type of product (Consumable, NonConsumable, Subscription)
- `string StoreId` - The product's identifier in the store
- `decimal DefaultPrice` - The price returned by `GetPrice` while the store has no metadata for the product
- `string DefaultCurrency` - The currency returned by `GetStringCurrency` while the store has no metadata for the product

### `ProductCollection`
A collection of `ProductInfo` objects that can be configured in the Unity Editor.

### `IPurchaseResponse`
Contains information about a purchase operation.

#### Properties
- `Product Product` - The Unity IAP Product object
- `string ProductId` - The `Id` of the product in the `ProductCollection`
- `string TransactionId` - The transaction ID. On Google Play it is the `GPA.` order ID when the receipt has one
- `string Receipt` - The unified receipt of the order
- `string SignedTransaction` - The App Store JWS signed transaction, for server-side validation under StoreKit 2
- `int Quantity` - The purchased quantity, more than 1 for Google Play multi-quantity purchases
- `PurchaseStatus Status` - The Purchase Status (Success, Failure, Deferred)
- `PurchaseFailureReason? FailureReason` - The reason of a failed purchase or confirmation
- `bool IsAutoConfirm` - Indicates whether the purchase will be automatically confirmed by the system
- `bool IsRestored` - Indicates that the purchase did not come from a `Purchase` call in this session
- `string ErrorMessage` - The error message if the Purchase Status is Failure

### `IRestoreAdapter`
Restores purchases on the platforms where it is available. The first available adapter is used.

- `bool IsAvailable` - Indicates if the adapter can restore purchases on the current platform
- `void RestorePurchases(IPurchaseService purchaseService, Action<bool, string> callback)` - Starts restoring and reports the result

## Migrating from 2.x
- Unity IAP 5.3.1 or newer is required. The service uses the `StoreController` API instead of the deprecated
  `UnityPurchasing.Initialize` and `IStoreListener`.
- `IRestoreAdapter.RestorePurchases` takes the Unity IAP `IPurchaseService` instead of `IExtensionProvider`.
- `IProductInfo` has `DefaultPrice` and `DefaultCurrency`. A custom `IProductCollection` has to provide them.
- Purchases without `autoConfirm` stay pending in the store until `ConfirmPendingPurchase` is called. In 2.x they were
  finished as soon as they arrived.
- `OnPurchased` can be raised for purchases from an earlier session. Check `IsRestored` if the game shows a purchase
  result screen.
- Use `IPurchaseResponse.ProductId` to find the product in the collection. `Product.definition.id` is the store ID.

## Support
InAppFlex is free and MIT-licensed. If it saves you time and you would like to help it grow, you can support its
development on [DonationAlerts](https://www.donationalerts.com/r/danilchizhikov).

It is entirely optional: bug reports, ideas and pull requests help
just as much.

## License
MIT. See [LICENSE](LICENSE).