namespace DTech.InAppFlex
{
	public enum StoreFailureStage : byte
	{
		StoreConnection = 0,
		ProductsFetch = 1,
		PurchasesFetch = 2,
		PurchasesRestore = 3,
	}
}
