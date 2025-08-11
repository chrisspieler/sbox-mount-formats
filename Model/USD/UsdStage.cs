namespace Duccsoft.Formats.Usd;

public class UsdStage : IModelResourceFile
{
	public UsdStage( List<UsdPrim> prims )
	{
		_prims = prims;
	}

	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;
	public Model LoadModel() => Model.Cube;
}
