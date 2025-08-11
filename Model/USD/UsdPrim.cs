namespace Duccsoft.Formats.Usd;

/// <summary>
/// The primary container object in USD. 
/// </summary>
public class UsdPrim
{
	public record UsdAttribute( AttributeType Type, AttributeRole Role, bool IsArray, string Name, string ValueText );
	
	public SdfSpecifier Specifier { get; set; }
	public string Type { get; set; }
	public string Name { get; set; }
	public string ValueText { get; set; }
	public Dictionary<string, UsdAttribute> Attributes { get; } = [];
	public List<UsdPrim> Children { get; } = [];

	public void AddAttribute( AttributeType type, AttributeRole role, bool isArray, string name, string valueText )
	{
		Log.Info( $"\tAdding {(role == AttributeRole.None ? string.Empty : role + " ")}attribute {type.ToString()}{(isArray ? "[]" : string.Empty)} \"{name}\": {(valueText?.Length > 300 ? valueText.Substring(0, 300) + "..." : valueText)}" );
		Attributes[name] = new UsdAttribute( type, role, isArray, name, valueText );
	}

	public void AddChild( UsdPrim child )
	{
		Children.Add( child );
	}
}
