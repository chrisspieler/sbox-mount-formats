namespace Duccsoft.Formats.Usd;

/// <summary>
/// The primary container object in USD. 
/// </summary>
public class UsdPrim
{
	public SdfSpecifier Specifier { get; set; }
	public string Type { get; set; }
	public string Name { get; set; }
	public string ValueText { get; set; }
	public Dictionary<string, UsdAttribute> Attributes { get; } = [];
	public List<UsdPrim> Children { get; } = [];

	public IEnumerable<string> GetPropertyNames() => Attributes.Keys;
	public UsdAttribute GetAttribute( string name ) => Attributes.GetValueOrDefault(name);

	public void AddAttribute( AttributeType type, AttributeRole role, bool isArray, string name, object value )
	{
		// Log.Info( $"\tAdding {(role == AttributeRole.None ? string.Empty : role + " ")}attribute {type.ToString()}{(isArray ? "[]" : string.Empty)} \"{name}\": {(value?.ToString()?.Length > 300 ? value?.ToString()?.Substring(0, 300) + "..." : value)}" );
		Attributes[name] = new UsdAttribute( type, role, isArray, name, value );
	}

	public void AddChild( UsdPrim child )
	{
		Children.Add( child );
	}
}
