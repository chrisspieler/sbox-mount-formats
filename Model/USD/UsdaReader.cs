using System.Diagnostics.CodeAnalysis;
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
		var tokens = new Queue<Token>(); 
		tokens.Enqueue( new Token( TokenType.Comment, 0, 0, headerLine!.Length ) );
		
		while ( reader.ReadLine() is { } line ) { lines.Add( line ); }
		
		for ( int i = 1; i < lines.Count; i++ )
		{
			TokenizeLine( i, lines[i], tokens );
		}

		Log.Info( $"Read {tokens.Count} tokens from USDA file, {tokens.Count( t => t.Type == TokenType.Label )} labels, {tokens.Count( t => t.Type == TokenType.OpBinaryAssign)} assignments, {tokens.Count( t => t.Type == TokenType.LiteralInt)} ints, {tokens.Count(t => t.Type == TokenType.LiteralFloat)} floats,  {tokens.Count( t => t.Type == TokenType.LiteralString )} strings" );
		
		// TODO: Build a tree using the tokens we've parsed.
		
		return Parse( tokens, lines );
	}

	private void TokenizeLine( int lineNum, string line, Queue<Token> tokens )
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
					
					// Sometimes, there will be an attribute name with sections separate by colons.
					// Should this be a different type of token instead of part of the label?
					if ( line[position] == ':' ) { continue; }
					
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
				EndToken();
			}

			tokenType = newType;
			tokenStartPosition = position;
		}

		void EndToken( int lengthOffset = 0 )
		{
			if ( tokenType == TokenType.None )
				return;

			var tokenLength = position - tokenStartPosition + lengthOffset;
			tokens.Enqueue( new Token( tokenType, lineNum, tokenStartPosition, tokenLength ) );
			tokenType = TokenType.None;
		}
	}

	private UsdStage Parse( Queue<Token> tokens, List<string> lines )
	{
		List<UsdPrim> allPrims = [];
		Stack<UsdPrim> primStack = [];

		while ( tokens.TryDequeue( out var currentToken ) )
		{
			Log.Info( $"({currentToken.Line},{currentToken.Position},{currentToken.Length}) {currentToken.Type.ToString()}" );
			
			// We don't need to do anything to comments.
			if ( currentToken.Type == TokenType.Comment )
				continue;

			if ( currentToken.Type == TokenType.ParenLeft )
			{
				// Skip over the layer metadata for now.
				// TODO: Read metadata for the layer.
				SkipScope( TokenType.ParenLeft, TokenType.ParenRight );
				continue;
			}

			// Have we reached the end of a prim?
			if ( currentToken.Type == TokenType.BraceRight )
			{
				primStack.Pop();
				continue;
			}
			
			Assert.AreEqual( currentToken.Type, TokenType.Label, "Expected prim specifier or type name!" );
				
			if ( TryParseSpecifier( TokenText( currentToken ), out var specifier ) )
			{
				var primType = ReadLabel();
				var primName = ReadStringLiteral();
				AddPrim( specifier.Value, primType, primName );
				// If this prim has metadata...
				if ( tokens.Peek().Type == TokenType.ParenLeft )
				{
					tokens.Dequeue();
					// ...completely ignore it for now.
					SkipScope( TokenType.ParenLeft, TokenType.ParenRight );
					// TODO: Parse prim metadata instead of skipping over it.
				}

				Assert.AreEqual( tokens.Dequeue().Type, TokenType.BraceLeft, "Expected left brace immediately after prim!" );
				
				// Continue the loop, as the next element may be either a prim or an attribute.
				continue;
			}
			
			// If we're not parsing a prim, then we're parsing an attribute.
			var typeName = TokenText( currentToken );

			// If we aren't reading the type name, then keep going.
			while ( typeName is "uniform" or "custom" )
				typeName = ReadLabel();

			var isArray = tokens.Peek().Type == TokenType.BracketLeft; 
			if ( isArray )
			{
				// Dequeue the square brackets.
				tokens.Dequeue();
				Assert.AreEqual( tokens.Dequeue().Type, TokenType.BracketRight, "Expected right square bracket in array definition." );
			}
			
			var attributeName = ReadLabel();
			primStack.Peek().AddAttribute( typeName, isArray, attributeName );

			// There may be a TokenType.OpBinaryAssign here, or the line may immediately end.
			
			// TODO: Actually read the values of the attributes. 
			SkipAttribute( currentToken.Line );
		}

		Assert.AreEqual( primStack.Count, 0, "Finished reading document without reaching end of prim!" );
		return new UsdStage( allPrims );

		string TokenText( Token token )
		{
			return lines[token.Line].Substring( token.Position, token.Length );
		}

		string ReadLabel() => TokenText( tokens.Dequeue() );
		string ReadStringLiteral() => ReadLabel().Trim( '"');

		void SkipScope( TokenType scopeBeginToken, TokenType scopeEndToken )
		{
			var depth = 1;
			do
			{
				var currentToken = tokens.Dequeue();
				if ( currentToken.Type == scopeBeginToken )
				{
					depth++;
				}
				else if ( currentToken.Type == scopeEndToken )
				{
					depth--;
				}
			} while ( depth > 0 );
		}

		void SkipAttribute( int lineNumber )
		{
			var lastToken = TokenType.None;
			while ( tokens.Count > 0 && tokens.Peek().Line <= lineNumber )
			{
				lastToken = tokens.Dequeue().Type;
			}

			// If this attribute has no metadata, we've skipped it just by going to the end of the line.
			if ( lastToken != TokenType.ParenLeft )
				return;

			// If there is metadata, keep skipping tokens until we see a right parenthesis.
			while ( tokens.Count > 0 && tokens.Dequeue().Type != TokenType.ParenRight ) { }
		}

		void AddPrim( SdfSpecifier specifier, string primType, string primName )
		{
			var prim = new UsdPrim()
			{
				Specifier = specifier,
				Type = primType,
				Name = primName
			};
			allPrims.Add( prim );
			if ( primStack.TryPeek( out var parent ) )
			{
				parent.AddChild( prim );
			}
			primStack.Push( prim );
		}
	}

	private bool TryParseSpecifier( string line, [NotNullWhen(true)]out SdfSpecifier? specifier )
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
