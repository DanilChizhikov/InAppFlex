using System;
using UnityEngine;

namespace DTech.InAppFlex
{
	[Serializable]
	internal sealed class ProductStoreId : IProductStoreId
	{
		[field: SerializeField] public RuntimePlatform Platform { get; private set; }
		[field: SerializeField] public string StoreId { get; private set; }
	}
}