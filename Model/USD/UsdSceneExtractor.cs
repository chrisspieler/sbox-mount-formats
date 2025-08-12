namespace Duccsoft.Formats.Usd;

public class UsdSceneExtractor( UsdStage usd )
{
	public UsdStage Stage { get; } = usd;

	public Scene GetScene( string name, UsdSceneSettings settings = null )
	{ 
		return new Scene { Name = name };
	}
}
