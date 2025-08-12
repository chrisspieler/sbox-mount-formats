namespace Duccsoft.Formats.Usd;

public class UsdStage
{
	public UsdStage( List<UsdPrim> prims )
	{
		_prims = prims;
	}

	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;
}
