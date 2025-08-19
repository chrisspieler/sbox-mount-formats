using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Ascii;

public class TokenReader( IReadOnlyList<Token> tokens )
{
	private const string VectorNoStartParenthesisError = "Expected left parenthesis at start of vector.";
	private const string VectorNoEndParenthesisError = "Expected right parenthesis at end of vector.";
	private const string VectorNoCommaError = "Expected comma between vector components.";

	/// <summary>
	/// Determines the position of the cursor used to read the underlying set of tokens.
	/// </summary>
	public int Position
	{
		get => _position;
		set
		{
			if ( value >= Length )
				throw new IndexOutOfRangeException( $"Given position {value} would exceed max position {Length - 1}" );
			
			_position = value;
		}
	}
	private int _position;

	/// <summary>
	/// Returns the total number of tokens.
	/// </summary>
	public int Length => tokens.Count;
	
	/// <summary>
	/// Returns the token at <see cref="Position"/> without advancing the position.
	/// </summary>
	public Token? Peek() => Position >= Length ? null : tokens[Position];
	
	/// <summary>
	/// Returns the token at <see cref="Position"/>, then advances the position to the next token.
	/// </summary>
	public Token? Read()
	{
		if ( Position >= Length )
			return null;
		
		var token = tokens[Position];
		_position++;
		return token;
	}

	public string ReadLabel() => Read()?.Text;
	public string ReadStringLiteral() => ReadLabel()?.Trim( '"' );

	public int ReadIntLiteral()
	{
		if ( Read() is not { Type: TokenType.LiteralInt } intToken )
			throw new Exception( "Expected int literal" );

		return int.Parse( intToken.Text );
	}
	
	public uint ReadUIntLiteral()
	{
		if ( Read() is not { Type: TokenType.LiteralInt } intToken )
			throw new Exception( "Expected int literal" );

		return uint.Parse( intToken.Text );
	}

	public float ReadFloatLiteral()
	{
		if ( Read() is not { Type: TokenType.LiteralFloat or TokenType.LiteralInt } floatToken )
			throw new Exception( "Expected float literal" );
		
		return float.Parse( floatToken.Text );
	}

	public Vector3 ReadVector3()
	{
		Assert.True( Read() is { Type: TokenType.ParenLeft }, VectorNoStartParenthesisError );

		var vector = Vector3.Zero;
		
		vector.x = ReadFloatLiteral();
		Assert.True( Read() is { Type: TokenType.Comma }, VectorNoCommaError );
		vector.y = ReadFloatLiteral();
		Assert.True( Read() is { Type: TokenType.Comma }, VectorNoCommaError );
		vector.z = ReadFloatLiteral();
		
		Assert.True( Read() is { Type: TokenType.ParenRight }, VectorNoEndParenthesisError );

		return vector;
	}
	
	public Vector2 ReadVector2()
	{
		Assert.True( Read() is { Type: TokenType.ParenLeft }, VectorNoStartParenthesisError );

		var vector = Vector2.Zero;
		
		vector.x = ReadFloatLiteral();
		Assert.True( Read() is { Type: TokenType.Comma }, VectorNoCommaError );
		vector.y = ReadFloatLiteral();
			
		Assert.True( Read() is { Type: TokenType.ParenRight }, VectorNoEndParenthesisError );

		return vector;
	}
	
	public object ReadValue( AttributeType dataType )
	{
		return dataType switch
		{
			AttributeType.Bool => ReadIntLiteral(),
			AttributeType.Int => ReadIntLiteral(),
			AttributeType.Uint => ReadUIntLiteral(),
			AttributeType.Float => ReadFloatLiteral(),
			AttributeType.Float3 => ReadVector3(),
			AttributeType.Float2 => ReadVector2(),
			_ => ReadValueAsString()
		};
	}

	public object ReadValueArray( AttributeType dataType )
	{
		return dataType switch
		{
			AttributeType.Int => ReadArray<int>().ToArray(),
			AttributeType.Float => ReadArray<float>().ToArray(),
			AttributeType.Float3 => ReadArray<Vector3>().ToArray(),
			AttributeType.Float2 => ReadArray<Vector2>().ToArray(),
			_ => ReadValueAsString()
		};

		IEnumerable<T> ReadArray<T>()
		{
			// Empty array.
			if ( Peek() is { Type: TokenType.Label } label && label.Text == "None" )
				yield break;
			
			Assert.AreEqual( Read()!.Value.Type, TokenType.BracketLeft );

			while ( Peek() is { Type: not TokenType.BracketRight } nextToken )
			{
				if ( nextToken.Type == TokenType.Comma )
				{
					Read();
					continue;
				}
				
				yield return (T)ReadValue( dataType );
			}
			// Read the right bracket.
			Read();
		}
	}
	
	public string ReadValueAsString()
	{
		if ( Read() is not { } firstToken )
			return string.Empty;

		return firstToken.Type switch
		{
			TokenType.BracketLeft => ReadArray(),
			TokenType.ParenLeft => ReadTuple(),
			_ => firstToken.Text
		};

		string ReadArray()
		{
			var sb = new StringBuilder();
			while ( Read() is { } token )
			{
				sb.Append( token.Text );
				if ( token.Type == TokenType.BracketRight ) { break; }
			}
			return sb.ToString();
		}

		string ReadTuple()
		{
			var sb = new StringBuilder();
			while ( Read() is { } token )
			{
				sb.Append( token.Text );
				if ( token.Type == TokenType.ParenRight ) { break; }
			}
			return sb.ToString();
		}
	}

	public void SkipCurrentMetadata() => SkipScope( TokenType.ParenLeft, TokenType.ParenRight );
	public void SkipCurrentPrim() => SkipScope( TokenType.BraceLeft, TokenType.BraceRight );
	public void SkipCurrentArray() => SkipScope( TokenType.BracketLeft, TokenType.BracketRight );

	private void SkipScope( TokenType scopeBeginToken, TokenType scopeEndToken )
	{
		var depth = 1;
		do
		{
			if ( Read() is not { } currentToken )
				throw new Exception( "Skipped scope did not end before end of document." );
			
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

	public void SkipCurrentLine( bool skipMetadata )
	{
		// If there are no tokens left, there's nothing to skip.
		if ( Peek() is not { } initialToken )
			return;

		var lastReadToken = TokenType.None;
		// Until we know for sure that the next token is on the same line, use Peek rather than Read.
		while ( Peek() is { } nextToken && nextToken.Line <= initialToken.Line )
		{
			lastReadToken = Read()!.Value.Type;
		}

		// If the last token in a line is a left parenthesis, then this attribute has metadata.
		var attributeHasMetadata = lastReadToken == TokenType.ParenLeft;
		if ( !attributeHasMetadata || !skipMetadata)
			return;

		SkipCurrentMetadata();
	}
}
