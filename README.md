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
- [API Reference](#api-reference)
  - [IInAppPurchaseService](#iinapppurchaseservice)
  - [IPurchaseResponse](#ipurchaseresponse)
  - [ProductInfo](#productinfo)
  - [ProductCollection](#productcollection)
  - [IPurchaseResponse](#ipurchaseresponse)
- [License](#license)


## Getting Started
Prerequisites:
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 2022.3+
- [Unity Purchasing](https://docs.unity3d.com/Manual/com.unity.purchasing.html) 4.13.0+

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

If you want to set a target version, InAppFlex uses the `v*.*.*` release tag so you can specify a version like #v2.0.0.

For example `https://github.com/DanilChizhikov/InAppFlex.git#v2.0.0`.

## Features

- 🛒 **Cross-Platform Support**: Works with both iOS and Android in-app purchases
- 🔄 **Purchase Restoration**: Built-in support for restoring purchases across devices
- 🔄 **Asynchronous Operations**: Non-blocking purchase flow with event-based callbacks
- 🏷️ **Product Management**: Easy management of in-app products with platform-specific store IDs
- 🔍 **Subscription Support**
- 💰 **Price Information**: Get localized prices and currency codes
- 🛡️ **Error Handling**: Comprehensive error handling and purchase validation
- 🔌 **Extensible Architecture**: Support for custom restore adapters

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
    _purchaseService.OnPurchasesRestored += OnPurchasesRestored;
    
    _purchaseService.Initialize();
}
```

### Making a Purchase

```csharp
public void PurchaseProduct(string productId)
{
    _purchaseService.Purchase(productId);
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

## API Reference
### `IInAppPurchaseService`

#### Properties
- `bool IsInitialized` - Indicates if the service is ready to process purchases

#### Events
- `event Action OnInitialized` - Triggered when the service is successfully initialized
- `event Action<InitializationFailureException> OnInitializeFailed` - Triggered when initialization fails
- `event Action<IPurchaseResponse> OnPurchased` - Triggered when a purchase is successful
- `event Action<bool> OnPurchasesRestored` - Triggered when restore purchases operation completes
- `event Action<IPurchaseResponse> OnPurchaseFailed` - Triggered when a purchase fails

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
- `IProductStoreId StoreId` - Platform-specific store identifiers

### `ProductCollection`
A collection of `ProductInfo` objects that can be configured in the Unity Editor.

### `IPurchaseResponse`
Contains information about a purchase operation.

#### Properties
- `Product Product` - The Unity IAP Product object
- `string TransactionId` - The Transaction ID from `Product`
- `string Receipt` - The Receipt from `Product`
- `PurchaseStatus Status` - The Purchase Status (Success, Failure)
- `bool IsAutoConfirm` - Indicates whether the purchase will be automatically confirmed by the system
- `string ErrorMessage` - Messaga if Purchase Status is Failure

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.