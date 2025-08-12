using System.IO;
using Duccsoft.Formats.Usd.Ascii;
using Duccsoft.Formats.Usd.Crate;

namespace Duccsoft.Formats.Usd;

public class UsdStage
{
	public UsdStage( List<UsdPrim> prims )
	{
		_prims = prims;
	}

	public static UsdStage LoadFromFile( string filePath )
	{
		return Path.GetExtension( filePath ) == ".usda" 
			? new UsdaReader().ReadFromPath( filePath ) 
			: new UsdcReader().ReadFromPath( filePath );
	}
	
	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;
}
