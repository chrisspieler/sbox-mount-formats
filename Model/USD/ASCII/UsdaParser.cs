using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Ascii;

/// <summary>
/// Creates a USD stage from a set of tokens and the lines they were read from.
/// </summary>
public class UsdaParser( IReadOnlyList<Token> tokens, IReadOnlyList<string> lines )
{
	public IReadOnlyList<string> Lines { get; } = lines;

	public UsdStage Parse()
	{
		// A flat layout of every prim that will be contained in the output stage.
		// TODO: Make this a Dictionary so I can use the absolute path as a key.
		List<UsdPrim> allPrims = [];
		
		// This stack defines the hierarchy of prims at the current cursor position. The bottom element of primStack
		// is the root prim that contains all other prims within a stage. Parsed metadata and attributes will always be
		// written to the top element of primStack. Once all attributes of a prim have been read, that prim will be
		// popped from the stack.
		Stack<UsdPrim> primStack = [];

		var reader = new TokenReader( tokens, Lines );
		while ( reader.Read() is { } currentToken )
		{
			Log.Info( $"({currentToken.Line + 1},{currentToken.Position + 1},{currentToken.Length + 1}) {currentToken.Type.ToString()}" );

			switch (currentToken.Type)
			{
				// We don't need to do anything to comments.
				case TokenType.Comment:
					continue;
				case TokenType.ParenLeft:
					// Skip over the stage metadata for now.
					// TODO: Read metadata for the stage.
					reader.SkipCurrentMetadata();
					continue;
				// Have we reached the end of a prim?
				case TokenType.BraceRight:
					Log.Info( $"Popping prim \"{primStack.Peek().Name}\"" );
					primStack.Pop();
					continue;
				case TokenType.LiteralString:
				case TokenType.LiteralFloat:
				case TokenType.LiteralInt:
				case TokenType.Comma:
					// This is probably an array spanning multiple lines, so ignore this line.
					// TODO: Remove this when I actually parse attribute values.
					reader.SkipCurrentLine( skipMetadata: true );
					continue;
			}
			
			Assert.AreEqual( currentToken.Type, TokenType.Label, $"Expected prim specifier or type name, but got {currentToken.Type}!" );
				
			if ( currentToken.AsSpecifier() is { } specifier )
			{
				var primType = "(override)";
				if ( specifier == SdfSpecifier.SdfSpecifierDef )
				{
					primType = reader.ReadLabel();
				}
				var primName = reader.ReadStringLiteral();
				AddPrim( specifier, primType, primName );
				// If this prim has metadata...
				if ( reader.Peek() is { Type: TokenType.ParenLeft } )
				{
					reader.Read();
					// ...completely ignore it for now.
					reader.SkipCurrentMetadata();
					// TODO: Parse prim metadata instead of skipping over it.
				}

				Assert.AreEqual( reader.Read()?.Type, TokenType.BraceLeft, "Expected left brace immediately after prim!" );
				
				// Continue the loop, as the next element may be either a prim or an attribute.
				continue;
			}
			
			// If we're not parsing a prim, then we're parsing an attribute.
			var typeName = currentToken.Text;

			// If the next label wasn't a type name, keep getting the next label until it's a type name. 
			while ( typeName is "uniform" or "custom" )
				typeName = reader.ReadLabel();

			var isArray = reader.Peek() is { Type: TokenType.BracketLeft }; 
			if ( isArray )
			{
				// Dequeue the square brackets.
				reader.Read();
				Assert.AreEqual( reader.Read()?.Type, TokenType.BracketRight, "Expected right square bracket in array definition." );
			}
			
			var attributeName = reader.ReadLabel();
			primStack.Peek().AddAttribute( typeName, isArray, attributeName );

			// There may be a TokenType.OpBinaryAssign here, or the line may immediately end.
			
			// TODO: Actually read the values of the attributes. 
			reader.SkipCurrentLine( skipMetadata: true );
		}

		Assert.AreEqual( primStack.Count, 0, "Finished reading document without reaching end of prim!" );
		return new UsdStage( allPrims );

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

			Log.Info( $"Adding {specifier.ToString()} prim {primType} \"{primName}\"" );
			primStack.Push( prim );
		}
	}
}
