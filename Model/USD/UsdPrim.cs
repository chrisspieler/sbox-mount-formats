namespace Duccsoft.Formats.Usd;

/// <summary>
/// The primary container object in USD. 
/// </summary>
public class UsdPrim
{
	public record UsdAttribute( string Type, bool IsArray, string Name, string ValueText );
	
	public SdfSpecifier Specifier { get; set; }
	public string Type { get; set; }
	public string Name { get; set; }
	public string ValueText { get; set; }
	public Dictionary<string, UsdAttribute> Attributes { get; } = [];
	public List<UsdPrim> Children { get; } = [];

	public void AddAttribute( string typeName, bool isArray, string attributeName, string valueText )
	{
		Log.Info( $"\tAdding attribute {typeName}{(isArray ? "[]" : string.Empty)} \"{attributeName}\": {(valueText?.Length > 300 ? valueText.Substring(0, 300) + "..." : valueText)}" );
		Attributes[attributeName] = new UsdAttribute( typeName, isArray, attributeName, valueText );
	}

	public void AddChild( UsdPrim child )
	{
		Children.Add( child );
	}
}
