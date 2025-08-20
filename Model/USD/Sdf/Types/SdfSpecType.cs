namespace Duccsoft.Formats.Usd;

/// <summary>
/// Specifies the type used by an "object". Objects are entities that have fields and are addressable by path.
/// </summary>
public enum SdfSpecType : uint
{
	SdfSpecTypeUnknown = 0,
	SdfSpecTypeAttribute,
	SdfSpecTypeConnection,
	SdfSpecTypeExpression,
	SdfSpecTypeMapper,
	SdfSpecTypeMapperArg,
	SdfSpecTypePrim,
	SdfSpecTypePseudoRoot,
	SdfSpecTypeRelationship,
	SdfSpecTypeRelationshipTarget,
	SdfSpecTypeVariant,
	SdfSpecTypeVariantSet
}
