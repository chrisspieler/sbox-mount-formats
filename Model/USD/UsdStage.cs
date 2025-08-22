using System.IO;
using Duccsoft.Formats.Usd.Ascii;
using Duccsoft.Formats.Usd.Crate;

namespace Duccsoft.Formats.Usd;

public class UsdStage
{
	public UsdStage( List<UsdPrim> prims )
	{ 
		_prims = prims;
		PseudoRoot = prims.FirstOrDefault();
	}
	
	public UsdStage( List<UsdPrim> prims, UsdPrim pseudoRoot )
	{ 
		_prims = prims;
		PseudoRoot = pseudoRoot;
	}

	public UsdPrim PseudoRoot { get; }

	public static UsdStage LoadFromFile( string filePath )
	{
		if ( Path.GetExtension( filePath ) == ".usda" )
			return new UsdaReader().ReadFromPath( filePath );

		var sdfLayer = SdfLayer.Load( filePath );
		return Open( sdfLayer );
	}

	public static UsdStage Open( SdfLayer rootLayer )
	{
		var primList = new List<UsdPrim>();
		var pseudoRoot = ComposePrimRecursive( rootLayer.PseudoRoot );
		return new UsdStage( primList, pseudoRoot );

		UsdPrim ComposePrimRecursive( SdfPrimSpec spec )
		{
			var prim = new UsdPrim
			{
				Specifier = spec.Specifier,
				Name = spec.Name
			};

			primList.Add( prim );
			
			foreach ( var child in spec.NameChildren )
			{
				prim.Children.Add( ComposePrimRecursive( child ) );
			}
			return prim;
		}
	}
	
	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;
}
