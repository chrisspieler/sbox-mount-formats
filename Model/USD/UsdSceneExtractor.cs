namespace Duccsoft.Formats.Usd;

public class UsdSceneExtractor()
{
	public Scene GetScene( UsdStage usdStage, string name, UsdSceneSettings settings = null )
	{
		var scene = new Scene { Name = name };
		Log.Info( $"Extracting scene {name} with {usdStage.Prims.Count} prims" );
		CreatePrimGameObjects( [usdStage.PseudoRoot] );
		Log.Info( $"Scene has {scene.Children.Count} children" );
		return scene;

		void CreatePrimGameObjects( IReadOnlyList<UsdPrim> prims, GameObject parent = null )
		{
			foreach ( var prim in prims )
			{
				var primGo = scene.CreateObject();
				primGo.Name = prim.Name;
				primGo.Parent = parent;
				Log.Info( $"prim {primGo.Name}, parent: {primGo.Parent?.Name ?? "null"}" );
				CreatePrimGameObjects( prim.Children, primGo );
			}
		}
	}
}
