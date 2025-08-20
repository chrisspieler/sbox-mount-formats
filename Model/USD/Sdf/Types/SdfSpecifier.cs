namespace Duccsoft.Formats.Usd;

/// <summary>
/// An enum that identifies the possible specifiers for an SdfPrimSpec.
/// </summary>
public enum SdfSpecifier
{
	/// <summary>
	/// Defines a concrete prim.
	/// </summary>
	SdfSpecifierDef,
	/// <summary>
	/// Overrides an existing prim.
	/// </summary>
	SdfSpecifierOver,
	/// <summary>
	/// Defines an abstract prim.
	/// </summary>
	SdfSpecifierClass
}
