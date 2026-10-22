using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	[Serializable]
	internal sealed class ProductInfo : IProductInfo
	{
		[field: SerializeField] public string Id { get; private set; }
		[field: SerializeField] public ProductType Type { get; private set; }
		[field: SerializeField] public string StoreId { get; private set; }
		[field: SerializeField, Min(0f)] public float DefaultPrice { get; private set; }
		[field: SerializeField] public string DefaultCurrency { get; private set; }
	}
}