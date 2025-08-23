using System.IO;
using Duccsoft.Formats.Usd.Ascii;

namespace Duccsoft.Formats.Usd;

public class UsdStage
{
	// TODO: Remove this - it's used only by the old USDA parser.
	public UsdStage( List<UsdPrim> prims )
	{ 
		_prims = prims;
		PseudoRoot = prims.FirstOrDefault();
	}
	
	public UsdStage( SdfLayer rootLayer )
	{ 
		_prims = new List<UsdPrim>();
		PseudoRoot = ComposePrimRecursive( rootLayer.PseudoRoot );
		RootLayer = rootLayer;
		return;

		UsdPrim ComposePrimRecursive( SdfPrimSpec spec )
		{
			var prim = new UsdPrim
			{
				Specifier = spec.Specifier,
				Name = spec.Name
			};

			_prims.Add( prim );
			
			foreach ( var child in spec.NameChildren )
			{
				prim.Children.Add( ComposePrimRecursive( child ) );
			}
			return prim;
		}
	}
	
	public UsdPrim PseudoRoot { get; }
	public SdfLayer RootLayer { get; }

	public static UsdStage Open( string filePath )
	{
		// TODO: Remove this after implementing SdfUsdaFileFormat
		if ( Path.GetExtension( filePath ) == ".usda" )
			return new UsdaReader().ReadFromPath( filePath );

		var sdfLayer = SdfLayer.CreateNew( filePath );
		return Open( sdfLayer );
	}

	public static UsdStage Open( SdfLayer rootLayer ) => new( rootLayer );
	
	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;
}
