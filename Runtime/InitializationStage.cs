namespace DTech.InAppFlex
{
    internal enum InitializationStage : byte
    {
        None = 0,
        Connecting = 1,
        FetchingProducts = 2,
        FetchingPurchases = 3,
    }
}