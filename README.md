# InAppFlex
![](https://img.shields.io/badge/unity-2022.3+-000.svg)

## Description
This repository contains the source code for an In-App Purchase Service,
which provides functionality for handling in-app purchases within the Unity game engine using the UnityEngine.Purchasing library.


## Table of Contents
- [Getting Started](#Getting-Started)
    - [Prerequisites](#prerequisites)
    - [Install manually (using .unitypackage)](#Install-manually-(using-.unitypackage))
    - [Install via UPM (using Git URL)](#Install-via-UPM-(using-Git-URL))
- [Features](#Features)
- [Basic Usage](#Basic-Usage)
  - [Setting up Products](#Setting-up-Products)
  - [Initializing the Service](#Initializing-the-Service)
  - [Making a Purchase](#Making-a-Purchase)
  - [Restoring Purchases](#Restoring-Purchases)
  - [Store Failures](#Store-Failures)
- [API Reference](#api-reference)
  - [IInAppPurchaseService](#iinapppurchaseservice)
  - [IPurchaseResponse](#ipurchaseresponse)
  - [ProductInfo](#productinfo)
  - [ProductCollection](#productcollection)
  - [StoreFailureException](#storefailureexception)
- [License](#license)


## Getting Started
Prerequisites:
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 2022.3+
- [Unity Purchasing](https://docs.unity3d.com/Manual/com.unity.purchasing.html) 5.3.1+

### Install manually (using .unitypackage)
1. Download the .unitypackage from [releases](https://github.com/DanilChizhikov/InAppFlex/releases/) page.
2. Import com.dtech.inappflex.x.x.x.unitypackage into your project.

### Install via UPM (using Git URL)
1. Open the manifest.json file in your project's Packages folder.
2. Add the following line to the dependencies section:
   ```json
   "com.dtech.inappflex": "https://github.com/DanilChizhikov/InAppFlex.git",
    ```
3. Unity will automatically import the package.

If you want to set a target version, InAppFlex uses the `v*.*.*` release tag so you can specify a version like #v3.0.0.

For example `https://github.com/DanilChizhikov/InAppFlex.git#v3.0.0`.

## Features

- 🛒 **Cross-Platform Support**: Works with both iOS and Android in-app purchases
- 🔄 **Purchase Restoration**: Built-in support for restoring purchases across devices
- 🔄 **Asynchronous Operations**: Non-blocking purchase flow with event-based callbacks and `Task` wrappers
- 🏷️ **Product Management**: Easy management of in-app products with platform-specific store IDs
- 🔍 **Subscription Support**
- 💰 **Price Information**: Get localized prices and currency codes
- 🛡️ **Error Handling**: Comprehensive error handling and purchase validation

## Basic Usage
### Setting up Products
Create a `ProductCollection` asset and add your in-app products:

1. Right-click in Project window → Create → DTech → InAppFlex → Product Collection
2. Add your products with their respective store IDs for different platforms
3. Configure product types (Consumable, Non-Consumable, Subscription)

### Initializing the Service

```csharp
// Create product collection reference
[SerializeField] private ProductCollection _productCollection;

// Initialize the service
private IInAppPurchaseService _purchaseService;

private async void Awake()
{
    _purchaseService = new InAppPurchaseService(_productCollection);
    _purchaseService.OnInitialized += OnInitialized;
    _purchaseService.OnInitializeFailed += OnInitializeFailed;
    _purchaseService.OnPurchased += OnPurchaseCompleted;
    _purchaseService.OnPurchaseFailed += OnPurchaseFailed;
    _purchaseService.OnPurchasesRestored += OnPurchasesRestored;

    bool isInitialized = await _purchaseService.InitializeAsync(destroyCancellationToken);
    Debug.Log(isInitialized ? "Purchasing is ready" : "Purchasing initialization failed");
}
```

Every operation is `Task` based, the events stay available for code that only observes the flow.
The events are raised before the awaited `Task` completes.

### Making a Purchase

```csharp
private async void PurchaseProduct(string productId)
{
    IPurchaseResponse response = await _purchaseService.PurchaseAsync(productId);
    if (response is { Status: PurchaseStatus.Success })
    {
        Debug.Log($"Purchase successful: {response.ProductId}");
    }
}

private void OnPurchaseCompleted(IPurchaseResponse response)
{
    Debug.Log($"Purchase successful: {response.ProductId}");
    
    // For non-consumable products, you might want to confirm the purchase
    if (!response.IsAutoConfirm)
    {
        _purchaseService.ConfirmPendingPurchase(response);
    }
}
```

`OnPurchased` also reports purchases that were left unconfirmed in a previous session.
Such a response always has `IsAutoConfirm == false`, so grant the content and call
`ConfirmPendingPurchase` to finish the order.

Pass `autoConfirm: true` to let the service confirm the order for you:

```csharp
IPurchaseResponse response = await _purchaseService.PurchaseAsync(productId, autoConfirm: true);
```

`PurchaseAsync` resolves with the same response on success and on failure, and returns `null` when the
purchase could not even be started, for example when the service is not initialized or the product is
missing from the `ProductCollection`.

### Restoring Purchases

```csharp
private async void RestorePurchases()
{
    bool isRestored = await _purchaseService.RestorePurchasesAsync();
    Debug.Log(isRestored ? "Purchases restored successfully" : "Failed to restore purchases");
}

private void OnPurchasesRestored(bool success)
{
    Debug.Log(success ? "Purchases restored successfully" : "Failed to restore purchases");
}
```

### Store Failures

Every `StoreController` failure that is not tied to a single purchase is reported through
`OnStoreFailed` as a `StoreFailureException`. Use `Stage` to tell them apart and
`IsRetryable` to decide whether retrying makes sense:

```csharp
_purchaseService.OnStoreFailed += OnStoreFailure;

private async void OnStoreFailure(StoreFailureException exception)
{
    Debug.LogWarning($"Store failed on {exception.Stage}: {exception.Message}");
    if (exception is { Stage: StoreFailureStage.StoreConnection, IsRetryable: true })
    {
        await _purchaseService.InitializeAsync();
    }
}
```

The event covers a store disconnect, a failed products fetch, a failed purchases fetch and a failed
restore. A failed restore raises it right before `OnPurchasesRestored(false)`.

Failures that happen while `InitializeAsync` is still running are reported through
`OnInitializeFailed` only, so a single failure never raises two events. A disconnect and a products
fetch failure therefore only reach `OnStoreFailed` after the service is initialized.

`IsInitialized` stays `true` when the store disconnects after initialization - the store may
reconnect on its own.

### Concurrency and Cancellation

Every operation may only be in flight once. Calling `InitializeAsync` or `RestorePurchasesAsync` while
the same operation is still running, or `PurchaseAsync` for a product that is already being purchased,
throws an `InvalidOperationException`.

A cancelled token only cancels the returned `Task`. The store operation behind it keeps running, so the
slot stays busy until the store answers - after cancelling `InitializeAsync` the next call throws until
the store responds, and `IsInitialized` still becomes `true` if the initialization eventually succeeds.

`Dispose` completes everything still awaiting: `InitializeAsync` and `RestorePurchasesAsync` with
`false`, `PurchaseAsync` with `null`.

## API Reference
### `IInAppPurchaseService`

#### Properties
- `bool IsInitialized` - Indicates if the service is ready to process purchases

#### Events
- `event Action OnInitialized` - Triggered when the service is successfully initialized
- `event Action<InitializationFailureException> OnInitializeFailed` - Triggered when initialization fails
- `event Action<StoreFailureException> OnStoreFailed` - Triggered on any store failure that is not tied to a single purchase
- `event Action<IPurchaseResponse> OnPurchased` - Triggered when a purchase is successful
- `event Action<bool> OnPurchasesRestored` - Triggered when restore purchases operation completes
- `event Action<IPurchaseResponse> OnPurchaseFailed` - Triggered when a purchase fails

#### Methods
- `Task<bool> InitializeAsync(CancellationToken token = default)` - Initializes the purchase service, returns whether the service is ready
- `Task<IPurchaseResponse> PurchaseAsync(string productId, bool autoConfirm = false, CancellationToken token = default)` - Initiates a purchase, returns `null` if the purchase could not be started
- `decimal GetPrice(string productId)` - Gets the price of a product
- `string GetStringCurrency(string productId)` - Gets the currency code for a product
- `void ConfirmPendingPurchase(IPurchaseResponse response)` - Confirms a pending purchase
- `bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo)` - Gets subscription information of an already fetched purchase
- `Task<bool> RestorePurchasesAsync(CancellationToken token = default)` - Restores previous purchases
- `void Dispose()` - Cleans up resources

### `ProductInfo`
Represents an in-app product.

#### Properties
- `string Id` - The product's unique identifier
- `ProductType Type` - The type of product (Consumable, NonConsumable, Subscription)
- `string StoreId` - Platform-specific store identifier

### `ProductCollection`
A collection of `ProductInfo` objects that can be configured in the Unity Editor.

### `IPurchaseResponse`
Contains information about a purchase operation.

#### Properties
- `Product Product` - The Unity IAP Product object
- `string ProductId` - The Product ID from `Product`
- `string TransactionId` - The Transaction ID from the `Order`
- `string Receipt` - The Receipt from the `Order`
- `PurchaseStatus Status` - The Purchase Status (Success, Failure)
- `bool IsAutoConfirm` - Indicates whether the purchase will be automatically confirmed by the system
- `string ErrorMessage` - Messaga if Purchase Status is Failure

### `StoreFailureException`
A store failure that is not tied to a single purchase.

#### Properties
- `StoreFailureStage Stage` - Where the failure happened (`StoreConnection`, `ProductsFetch`, `PurchasesFetch`, `PurchasesRestore`)
- `bool IsRetryable` - Whether repeating the operation may succeed
- `string Message` - The store's description of the failure

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.