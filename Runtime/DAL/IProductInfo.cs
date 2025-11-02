using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public interface IProductInfo
	{
		string Id { get; }
		ProductType Type { get; }
		IProductStoreId StoreId { get; }
	}
}