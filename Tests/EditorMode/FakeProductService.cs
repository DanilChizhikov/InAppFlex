using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex.Tests.EditorMode
{
    public sealed class FakeProductService : IProductService
    {
        public event Action<List<Product>> OnProductsFetched;
        public event Action<ProductFetchFailed> OnProductsFetchFailed;

        private readonly Dictionary<string, Product> _products = new();

        public IAppleStoreExtendedProductService Apple => null;
        public int FetchCalls { get; private set; }
        public List<ProductDefinition> LastRequested { get; private set; }

        public void AddProduct(Product product)
        {
            _products[product.definition.id] = product;
        }

        public void RemoveProduct(string storeId)
        {
            _products.Remove(storeId);
        }

        public void RemoveAllProducts()
        {
            _products.Clear();
        }

        public void FetchProductsWithNoRetries(List<ProductDefinition> productDefinitions)
        {
            FetchProducts(productDefinitions);
        }

        public void FetchProducts(List<ProductDefinition> productDefinitions, IRetryPolicy retryPolicy = null)
        {
            FetchCalls++;
            LastRequested = productDefinitions;
            var fetched = new List<Product>();
            var missing = new List<ProductDefinition>();
            foreach (ProductDefinition definition in productDefinitions)
            {
                if (_products.TryGetValue(definition.id, out Product product))
                {
                    fetched.Add(product);
                }
                else
                {
                    missing.Add(definition);
                }
            }

            if (fetched.Count > 0)
            {
                OnProductsFetched?.Invoke(fetched);
            }

            if (missing.Count > 0)
            {
                OnProductsFetchFailed?.Invoke(StoreFixtures.CreateProductFetchFailed(missing, "products not found"));
            }
        }

        public ReadOnlyObservableCollection<Product> GetProducts()
        {
            return new ReadOnlyObservableCollection<Product>(new ObservableCollection<Product>(_products.Values));
        }

        public Product GetProductById(string productId)
        {
            return _products.TryGetValue(productId, out Product product) ? product : null;
        }
    }
}