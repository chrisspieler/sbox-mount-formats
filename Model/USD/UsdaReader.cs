using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats;

public class UsdaReader : IFileReader<UsdStage>
{
	private struct Token( TokenType type, int line, int position, int length )
	{
		public readonly TokenType Type = type;
		public readonly int Line = line;
		public readonly int Position = position;
		public readonly int Length = length;
	}
	
	private enum TokenType
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
		Comma
	}
	
	private const string UsdaHeader = "#usda 1.0";

	public const string DefSpecifier = "def";
	public const string OverSpecifier = "over";
	public const string ClassSpecifier = "class";
	
	private int _parenthesisDepth = 0;
	private int _curlyBracketDepth = 0;
	private int _squareBracketDepth = 0;
	
	public UsdStage ReadFromPath( string filePath ) => ReadFromBytes( File.ReadAllBytes( filePath ) );
	public UsdStage ReadFromBytes( byte[] bytes )
	{
		using var reader = new StreamReader( new MemoryStream( bytes ) );

		var headerLine = reader.ReadLine();
		Assert.AreEqual( headerLine, UsdaHeader, "Unable to parse USDA header!" );

		var lines = new List<string> { headerLine };
		var tokens = new List<Token> { new( TokenType.Comment, 0, 0, headerLine!.Length ) };
		
		while ( reader.ReadLine() is { } line ) { lines.Add( line ); }
		
		for ( int i = 1; i < lines.Count; i++ )
		{
			ReadLineTokens( i, lines[i], tokens );
		}

		Log.Info( $"Read {tokens.Count} tokens from USDA file, {tokens.Count( t => t.Type == TokenType.Label )} labels, {tokens.Count( t => t.Type == TokenType.OpBinaryAssign)} assignments, {tokens.Count( t => t.Type == TokenType.LiteralInt)} ints, {tokens.Count(t => t.Type == TokenType.LiteralFloat)} floats,  {tokens.Count( t => t.Type == TokenType.LiteralString )} strings" );
		
		// TODO: Build a tree using the tokens we've parsed.
		
		return new UsdStage();
	}

	private void ReadLineTokens( int lineNum, string line, List<Token> tokens )
	{
		var tokenType = TokenType.None;
		var tokenStartPosition = 0;
		
		var position = 0;
		for ( ; position < line.Length; position++ )
		{
			// For tokens that span multiple columns, first check to see whether they should end on this column.
			switch ( tokenType )
			{
				case TokenType.LiteralString:
					if ( line[position] == '"' )
					{
						EndToken();
					}
					continue;
				case TokenType.Label:
					// Alphanumeric characters continue a label.
					if ( char.IsAsciiLetterOrDigit( line[position] ) ) { continue; }
					break;
				case TokenType.LiteralInt:
					// Digits continue an int.
					if ( char.IsDigit( line[position] ) ) { continue; }
					
					// A decimal 'promotes' the int to a float.
					if ( line[position] == '.' )
					{
						tokenType = TokenType.LiteralFloat;
						continue;
					}
					break;
				case TokenType.LiteralFloat:
					// Digits or 'e' continue a float.
					if ( line[position] == 'e' || char.IsDigit( line[position] ) ) { continue; }
					break;
			}

			// Comments are a special case where we immediately stop processing the line.
			if ( line[position] == '#' )
			{
				StartToken( TokenType.Comment );
				// The comment will be as long as the remainder of the line.
				EndToken( line.Length - position );
				break;
			}

			// Tokens that take up only one character are handled here.
			var nextToken = line[position] switch
			{
				'{' => TokenType.BraceLeft,
				'}' => TokenType.BraceRight,
				'[' => TokenType.BracketLeft,
				']' => TokenType.BracketRight,
				'(' => TokenType.ParenLeft,
				')' => TokenType.ParenRight,
				'"' => TokenType.LiteralString,
				'=' => TokenType.OpBinaryAssign,
				',' => TokenType.Comma,
				// Numbers begin as ints, and only if they have a '.' or 'e' do we treat them as floats.
				'-' or >= '0' and <= '9' => TokenType.LiteralInt,
				// Text out in the open without quotes is just a label.
				>= 'A' and <= 'Z' => TokenType.Label,
				>= 'a' and <= 'z' => TokenType.Label,
				_ => TokenType.None
			};
			StartToken( nextToken );
		}

		// If we were in the middle of a token, finish capturing it now.
		EndToken( -1 );
		return;

		void StartToken( TokenType newType )
		{
			// If we've started a new token without ending the previous...
			if ( tokenType != TokenType.None )
			{
				// ...make the previous token end on the column before the start of this token.
				EndToken( -1 );
			}

			tokenType = newType;
			tokenStartPosition = position;
		}

		void EndToken( int lengthOffset = 0 )
		{
			if ( tokenType == TokenType.None )
				return;

			var tokenLength = position - tokenStartPosition + lengthOffset;
			tokens.Add( new Token( tokenType, lineNum, tokenStartPosition, tokenLength ) );
			tokenType = TokenType.None;
		}
	}

	private bool TryParseSpecifier( string line, out SdfSpecifier? specifier )
	{
		specifier = null;
		
		if ( line.StartsWith( DefSpecifier ) )
			specifier = SdfSpecifier.SdfSpecifierDef;
		if ( line.StartsWith( OverSpecifier ) )
			specifier = SdfSpecifier.SdfSpecifierOver;
		if ( line.StartsWith( ClassSpecifier ) )
			specifier = SdfSpecifier.SdfSpecifierClass;
		
		return specifier is not null;
	}
}
