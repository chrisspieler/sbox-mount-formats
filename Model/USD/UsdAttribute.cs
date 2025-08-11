namespace Duccsoft.Formats.Usd;

public record UsdAttribute( AttributeType Type, AttributeRole Role, bool IsArray, string Name, object Value )
{
	public bool TryGetValue<T>( out T value )
	{
		value = default;

		if ( Value is not T tValue )
			return false;

		value = tValue;
		return true;
	}
}
