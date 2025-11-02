using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	[Serializable]
	internal sealed class ProductInfo : IProductInfo
	{
		[SerializeField] private string _id = default;
		[SerializeField] private ProductType _type;
		[SerializeField] private ProductStoreId[] _storeIds = Array.Empty<ProductStoreId>();

		public string Id => _id;
		public ProductType Type => _type;

		public IProductStoreId StoreId
		{
			get
			{
				IProductStoreId result = null;
				foreach (var storeId in _storeIds)
				{
					if (storeId.Platform == Application.platform)
					{
						result = storeId;
						break;
					}
				}
				
				return result;
			}
		}
	}
}