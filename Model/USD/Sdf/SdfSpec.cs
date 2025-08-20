namespace Duccsoft.Formats.Usd;

public class SdfSpec
{
	public SdfSpec( SdfLayer layer, SdfPath path, SdfSpecType specType )
	{
		Layer = layer;
		Path = path;
		SpecType = specType;
	}
	
	public SdfLayer Layer { get; }
	public SdfPath Path { get; }
	public SdfSpecType SpecType { get; }

	private readonly Dictionary<TfToken, VtValue> _fields = [];

	public IEnumerable<TfToken> ListFields() => _fields.Keys;

	public bool HasField( TfToken name ) => GetField( name ) is not null;
	public VtValue GetField( TfToken name ) => _fields.GetValueOrDefault( name );
	// TODO: Try to cast the underlying VtValue to T
	public T GetFieldAs<T>( TfToken name, T defaultValue = default )
	{
		if ( !_fields.TryGetValue( name, out var value ) )
			return defaultValue;

		// TODO: Implement the conversion logic within VtValue itself.
		return defaultValue switch
		{
			string => (T)(object)value.ToString(),
			_ => defaultValue
		};
	}

	public bool SetField( TfToken name, VtValue value )
	{
		_fields[name] = value;
		return true;
	}

	// TODO: Create a VtValue from value
	public bool SetField<T>( TfToken name, T value ) => false;

	public bool ClearField( TfToken name ) => _fields.Remove( name );
}
