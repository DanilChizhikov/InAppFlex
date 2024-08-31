# InAppFlex
![](https://img.shields.io/badge/unity-2022.3+-000.svg)

## Description
This repository contains the source code for an In-App Purchase Service,
which provides functionality for handling in-app purchases within the Unity game engine using the UnityEngine.Purchasing library.


## Table of Contents
- [Getting Started](#Getting-Started)
    - [Install manually (using .unitypackage)](#Install-manually-(using-.unitypackage))
    - [Install via UPM (using Git URL)](#Install-via-UPM-(using-Git-URL))
- [Project Structure](#Project-Structure)
  - [Interfaces](#Interfaces)
  - [Restore Adapters](#Restore-Adapters)
- [Basic Usage](#Basic-Usage)
  - [Initialize](#Initialize)
  - [Purchasing](#Purchasing)
- [License](#License)

## Getting Started
Prerequisites:
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 2022.3+
- [Unity Purchasing](https://docs.unity3d.com/Manual/com.unity.purchasing.html) 4.10.0+

### Install manually (using .unitypackage)
1. Download the .unitypackage from [releases](https://github.com/DanilChizhikov/InAppFlex/releases/) page.
2. Open InAppFlex.x.x.x.unitypackage

### Install via UPM (using Git URL)
1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies": { line
    - ```json title="Packages/manifest.json"
      "com.danilchizhikov.inappflex": "https://github.com/DanilChizhikov/InAppFlex.git?path=Assets/InAppFlex",
      ```
UPM should now install the package.

## Project Structure

### Interfaces

1. IInAppService

```csharp
public interface IInAppService : IDisposable
{
    //Event triggered when a service initialized.
    event Action OnInitialized;
    //Event triggered when a service initialized with errors.
    event Action<InitializationFailureReason> OnInitializedFailed;
    //Event triggered when a purchase restored.
    event Action<bool> OnPurchasesRestored;
    //Event triggered when a purchase is made.
    event Action<IPurchaseResponse> OnPurchased;

    //Property to check if the service is initialized.
    bool IsInitialized { get; }
    
    //Method to initialize the service with a dictionary of products.
    void Initialize(Dictionary<ProductType, HashSet<string>> products);
    //Method to purchase a product identified by its ID.
    void Purchase(string productId, bool autoConfirm = false);
    //Method to get the price of a product.
    decimal GetPrice(string productId);
    //Method to get the currency of a product.
    string GetStringCurrency(string productId);
    //Method to confirm a pending purchase.
    void ConfirmPendingPurchase(IPurchaseResponse response);
    //Method to check subscription and returns true if subscription has or false if hasn't
    bool TryGetSubscriptionInfo(string productId, out SubscriptionInfo subscriptionInfo);
    //Method for restore purchases
    void RestorePurchases();
}
```

2. IPurchaseResponse

```csharp
public interface IPurchaseResponse
{
    //Purchased product.
    Product Product { get; }
    //Transaction ID of the purchase.
    string TransactionId { get; }
    //Receipt of the purchase.
    string Receipt { get; }
    //Status of the purchase (Processing, Success, Failure).
    PurchaseStatus Status { get; }
    //Error message in case of failure.
    string ErrorMessage { get; }
}
```

3. IRestoreAdapter

```csharp
public interface IRestoreAdapter
{
    bool IsAvailable { get; }
    
    //Method for restore purchases
    void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback);
}
```

### Restore Adapters

1. AppleRestoreAdapter
```csharp
public sealed class AppleRestoreAdapter : RestoreAdapter
{
    public override bool IsAvailable => Application.platform == RuntimePlatform.tvOS ||
                                        Application.platform == RuntimePlatform.VisionOS ||
                                        Application.platform == RuntimePlatform.IPhonePlayer;
    
    public override void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback)
    {
        var extension = provider.GetExtension<IAppleExtensions>();
        extension.RestoreTransactions(callback);
    }
}
```

2. GoogleRestoreAdapter
```csharp
public sealed class GoogleRestoreAdapter : RestoreAdapter
{
    public override bool IsAvailable => Application.platform == RuntimePlatform.Android;
    
    public override void RestorePurchases(IExtensionProvider provider, Action<bool, string> callback)
    {
        var extension = provider.GetExtension<IGooglePlayStoreExtensions>();
        extension.RestoreTransactions(callback);
    }
}
```

## Basic Usage

### Initialize
First, you need to initialize the InAppService, this can be done using different methods.
Here we will show the easiest way, which is not the method that we recommend using!
```csharp
public sealed class InAppServiceBootstrap : MonoBehaviour
{
    [Serializable]
    private struct ProductInfo
    {
        [SerializeField] private ProductType _type;
        [SerializeField] private string _productId;

        public ProductType Type => _type;
        public string ProductId => _productId;
    }

    [SerializeField] private ProductInfo[] _infos = Array.Empty<ProductInfo>();

    private static IInAppService _service;

    public static IInAppService Service => _service;

    private Dictionary<ProductType, HashSet<string>> GetProducts()
    {
        var products = new Dictionary<ProductType, HashSet<string>>();
        for (int i = _infos.Length - 1; i >= 0; i--)
        {
            ProductInfo productInfo = _infos[i];
            if (!products.TryGetValue(productInfo.Type, out HashSet<string> productIds))
            {
                productIds = new HashSet<string>();
                products.Add(productInfo.Type, productIds);
            }

            productIds.Add(productInfo.ProductId);
            products[productInfo.Type] = productIds;
        }

        return products;
    }

    private void Awake()
    {
        if (Service != null)
        {
            Destroy(gameObject);
            return;
        }

        var appleRestoreAdapter = new AppleRestoreAdapter();
        var googleRestoreAdapter = new GoogleRestoreAdapter();
        _service = new InAppService(new List<IRestoreAdapter>
                {
                        appleRestoreAdapter,
                        googleRestoreAdapter,
                });
        _service.Initialize(GetProducts());
    }
}
```

### Purchasing

Below is one of the possible options for making a purchase.

Example:
```csharp
internal sealed class PurchaseExample : IDisposable
{
    private const string ProductId = "Example_Product_Id";

    private readonly IInAppService _inAppService;

    public PurchaseExample(IInAppService inAppService)
    {
        _inAppService = inAppService;
    }

    public void Initialize()
    {
        _inAppService.OnPurchased += InAppPurchasedCallback;
    }

    public void Purchase()
    {
        _inAppService.Purchase(ProductId);
    }
    
    public void Dispose()
    {
        _inAppService.OnPurchased -= InAppPurchasedCallback;
    }
    
    private void InAppPurchasedCallback(IPurchaseResponse response)
    {
        if (response.Product.definition.id != ProductId)
        {
            return;
        }
        
        // some code...
    }
}
```

## License

MIT