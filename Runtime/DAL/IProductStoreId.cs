using UnityEngine;

namespace DTech.InAppFlex
{
	public interface IProductStoreId
	{
		RuntimePlatform Platform { get; }
		string StoreId { get; }
	}
}