namespace Duccsoft.Formats.Usd.Ascii;

public enum TokenType
{
	None,
	Comment,
	BraceLeft,
	BraceRight,
	BracketLeft,
	BracketRight,
	ParenLeft,
	ParenRight,
	/// <summary>
	/// e.g., an identifier, specifier, type name, or property name.
	/// </summary>
	Label,
	LiteralString,
	LiteralInt,
	LiteralFloat,
	OpBinaryAssign,
	Comma,
	Path
}
