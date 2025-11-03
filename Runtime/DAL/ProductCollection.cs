using System;
using UnityEngine;

namespace DTech.InAppFlex
{
	[CreateAssetMenu(fileName = nameof(ProductCollection), menuName = "DTech/InAppFlex/ProductCollection")]
	public sealed class ProductCollection : ScriptableObject, IProductCollection
	{
		[SerializeField] private ProductInfo[] _products = Array.Empty<ProductInfo>();

		public int Count => _products.Length;

		public IProductInfo this[int index] => _products[index];
	}
}