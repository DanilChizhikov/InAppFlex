namespace DTech.InAppFlex
{
	public interface IProductCollection
	{
		int Count { get; }
		IProductInfo this[int index] { get; }
	}
}