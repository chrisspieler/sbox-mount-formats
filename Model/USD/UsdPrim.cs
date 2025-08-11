namespace Duccsoft.Formats.Usd;

/// <summary>
/// The primary container object in USD. 
/// </summary>
public class UsdPrim
{
	public record UsdAttribute( string Type, bool IsArray, string Name );
	
	public SdfSpecifier Specifier { get; set; }
	public string Type { get; set; }
	public string Name { get; set; }
	public Dictionary<string, UsdAttribute> Attributes { get; } = [];
	public List<UsdPrim> Children { get; } = [];

	public void AddAttribute( string typeName, bool isArray, string attributeName )
	{
		Log.Info( $"\tAdding {typeName}{(isArray ? "[]" : string.Empty)} attribute \"{attributeName}\"" );
		Attributes[attributeName] = new UsdAttribute( typeName, isArray, attributeName );
	}

	public void AddChild( UsdPrim child )
	{
		Children.Add( child );
	}
}
