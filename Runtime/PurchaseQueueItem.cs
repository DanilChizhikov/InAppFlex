using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
	internal readonly struct PurchaseQueueItem
	{
		public readonly IProductInfo Info;
		public readonly Product Product;
		public readonly bool AutoConfirm;

		public PurchaseQueueItem(IProductInfo info, Product product, bool autoConfirm)
		{
			Info = info;
			Product = product;
			AutoConfirm = autoConfirm;
		}
	}
}