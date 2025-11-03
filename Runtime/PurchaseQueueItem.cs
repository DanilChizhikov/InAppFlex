namespace DTech.InAppFlex
{
	internal readonly struct PurchaseQueueItem
	{
		public readonly string ProductId;
		public readonly bool AutoConfirm;

		public PurchaseQueueItem(string productId, bool autoConfirm)
		{
			ProductId = productId;
			AutoConfirm = autoConfirm;
		}
	}
}