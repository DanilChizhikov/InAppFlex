using System;
using UnityEngine;

namespace DTech.InAppFlex
{
	[Serializable]
	internal sealed class ProductStoreId : IProductStoreId
	{
		[SerializeField] private RuntimePlatform _platform;
		[SerializeField] private string _storeId;

		public RuntimePlatform Platform => _platform;

		public override string ToString() => _storeId;
	}
}