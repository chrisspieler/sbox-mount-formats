namespace Duccsoft.Formats.Usd.Ascii;

public readonly struct Token( IReadOnlyList<string> lines, TokenType type, int line, int position, int length )
{
	private const string DefSpecifier = "def";
	private const string OverSpecifier = "over";
	private const string ClassSpecifier = "class";
	
	public readonly IReadOnlyList<string> Lines = lines;
	public readonly TokenType Type = type;
	public readonly int Line = line;
	public readonly int Position = position;
	public readonly int Length = length;
	
	/// <summary>
	/// Returns the text of a given token.
	/// </summary>
	public string Text => Lines[Line].Substring( Position, Length );
	
	public SdfSpecifier? AsSpecifier()
	{
		return Text switch
		{
			DefSpecifier => SdfSpecifier.SdfSpecifierDef,
			OverSpecifier => SdfSpecifier.SdfSpecifierOver,
			ClassSpecifier => SdfSpecifier.SdfSpecifierClass,
			_ => null
		};
	}
}
