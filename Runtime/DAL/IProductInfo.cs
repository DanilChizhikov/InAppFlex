using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	public interface IProductInfo
	{
		string Id { get; }
		ProductType Type { get; }
		string StoreId { get; }
		float DefaultPrice { get; }
		string DefaultCurrency { get; }
	}
}