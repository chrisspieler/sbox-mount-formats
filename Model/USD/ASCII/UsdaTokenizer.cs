using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Ascii;

internal class UsdaTokenizer
{
	private const string UsdaHeader = "#usda 1.0";
	
	public UsdaTokenizer( byte[] bytes, bool enableValidation = true )
	{
		using var reader = new StreamReader( new MemoryStream( bytes ) );

		_lines = [];
		while ( reader.ReadLine() is { } line ) { _lines.Add( line ); }

		if ( enableValidation )
		{
			Assert.AreEqual( _lines[0], UsdaHeader, "Unable to parse USDA header!" );
		}
	}

	public IReadOnlyList<string> Lines => _lines;
	private readonly List<string> _lines;

	public List<Token> TokenizeAll()
	{
		var tokens = new List<Token>(); 
		for ( int i = 1; i < _lines.Count; i++ )
		{
			tokens.AddRange( TokenizeLine( i, _lines[i] ) );
		}
		return tokens;
	}
	
	public IEnumerable<Token> TokenizeLine( int lineNum, string line )
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
						if ( EndToken( 1 ) is { } token ) { yield return token; }
					}
					continue;
				case TokenType.Label:
					var nextChar = line[position];
					// Alphanumeric characters continue a label.
					if ( char.IsAsciiLetterOrDigit( nextChar ) ) { continue; }
					
					// Some special characters may be included in a label.
					if ( nextChar is '_' or '-' or ':' or '.' ) { continue; }
					
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
				case TokenType.Path:
					// Paths only end with a right angle bracket.
					if ( line[position] == '>' )
					{
						if ( EndToken( 1 ) is { } pathToken ) yield return pathToken;
					}
					continue;
			}

			// Comments are a special case where we immediately stop processing the line.
			if ( line[position] == '#' )
			{
				if ( StartToken( TokenType.Comment ) is { } prevToken ) { yield return prevToken; }
				// The comment will be as long as the remainder of the line.
				if ( EndToken( line.Length - position ) is { } comment ) { yield return comment; }
				break;
			}

			// Check what token the next character would have us start reading.
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
				'<' => TokenType.Path,
				_ => TokenType.None
			};
			if ( StartToken( nextToken ) is { } preSingleToken ) { yield return preSingleToken; }
		}

		// If we were in the middle of a token, finish capturing it now.
		if ( EndToken( -1 ) is { } lastToken ) { yield return lastToken; }
		yield break;
		
		Token? StartToken( TokenType newType )
		{
			Token? token = null; 
			
			// If we've started a new token without ending the previous...
			if ( tokenType != TokenType.None )
			{
				// ...make the previous token end on the column before the start of this token.
				token = EndToken();
			}

			tokenType = newType;
			tokenStartPosition = position;
			return token;
		}
		
		Token? EndToken( int lengthOffset = 0 )
		{
			if ( tokenType == TokenType.None )
				return null;

			var tokenLength = position - tokenStartPosition + lengthOffset;
			var token = new Token( _lines, tokenType, lineNum, tokenStartPosition, tokenLength );
			tokenType = TokenType.None;
			return token;
		}
	}
}
