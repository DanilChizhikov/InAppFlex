# InAppFlex
[![Unity Version](https://img.shields.io/badge/unity-6000.0+-000.svg)](https://unity.com/releases/editor/archive)
![Unity Tests](https://github.com/DanilChizhikov/InAppFlex/actions/workflows/tests.yml/badge.svg?branch=master)
[![Donate](https://img.shields.io/badge/donate-DonationAlerts-f59c07.svg)](https://www.donationalerts.com/r/danilchizhikov)

## Overview
InAppFlex is a lightweight in-app purchase service for Unity built on top of
[Unity Purchasing](https://docs.unity3d.com/Manual/com.unity.purchasing.html). You describe your products in a
`ProductCollection` asset, create an `InAppPurchaseService`, and get events for initialization, purchases and
restoring. Purchases are queued and processed one at a time, pending purchases can be confirmed later, and restoring
is handled by pluggable adapters for the App Store and Google Play.

## Table of Contents
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [UPM Installation](#upm-installation)
- [Features](#features)
- [Usage](#usage)
    - [Setting up Products](#setting-up-products)
    - [Initializing the Service](#initializing-the-service)
    - [Making a Purchase](#making-a-purchase)
    - [Restoring Purchases](#restoring-purchases)
- [API Reference](#api-reference)
    - [IInAppPurchaseService](#iinapppurchaseservice)
    - [ProductInfo](#productinfo)
    - [ProductCollection](#productcollection)
    - [IPurchaseResponse](#ipurchaseresponse)
- [Support](#support)
- [License](#license)

## Getting Started

### Prerequisites
- [Unity](https://unity.com/releases/editor/archive) 6000.0+
- [Unity Purchasing](https://docs.unity3d.com/Manual/com.unity.purchasing.html) 4.13.0+

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
- 🔍 **Subscription Support**
- 💰 **Price Information**: Get localized prices and currency codes
- 🛡️ **Error Handling**: Comprehensive error handling and purchase validation
- 🔌 **Extensible Architecture**: Support for custom restore adapters

## Usage

### Setting up Products
Create a `ProductCollection` asset and add your in-app products:

1. Right-click in the Project window → Create → DTech → InAppFlex → ProductCollection
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
    Debug.Log($"Purchase successful: {response.Product.definition.id}");
    
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
- `string StoreId` - The product's identifier in the store

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
- `string ErrorMessage` - The error message if the Purchase Status is Failure

## Support
InAppFlex is free and MIT-licensed. If it saves you time and you would like to help it grow, you can support its
development on [DonationAlerts](https://www.donationalerts.com/r/danilchizhikov).

It is entirely optional: bug reports, ideas and pull requests help
just as much.

## License
MIT. See [LICENSE](LICENSE).