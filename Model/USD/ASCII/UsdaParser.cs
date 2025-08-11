using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Ascii;

/// <summary>
/// Creates a USD stage from a set of tokens and the lines they were read from.
/// </summary>
public class UsdaParser( IReadOnlyList<Token> tokens, IReadOnlyList<string> lines )
{
	public IReadOnlyList<string> Lines { get; } = lines;

	private static IReadOnlyDictionary<string, AttributeType> _attributeTypeNames =
		new Dictionary<string, AttributeType>
		{
			// Basic data types
			{ "bool", AttributeType.Bool },
			{ "uchar", AttributeType.UChar },
			{ "int", AttributeType.Int },
			{ "uint", AttributeType.Uint },
			{ "int64", AttributeType.Int64 },
			{ "uint64", AttributeType.Uint64 },
			{ "half", AttributeType.Half },
			{ "float", AttributeType.Float },
			{ "double", AttributeType.Double },
			{ "timecode", AttributeType.Timecode },
			{ "string", AttributeType.String },
			{ "token", AttributeType.Token },
			{ "asset", AttributeType.Asset },
			{ "opaque", AttributeType.Opaque },
			{ "matrix2d", AttributeType.Matrix2d },
			{ "matrix3d", AttributeType.Matrix3d },
			{ "matrix4d", AttributeType.Matrix4d },
			{ "quatd", AttributeType.Quatd },
			{ "quatf", AttributeType.Quatf },
			{ "quath", AttributeType.Quath },
			{ "double2", AttributeType.Double2 },
			{ "float2", AttributeType.Float2 },
			{ "half2", AttributeType.Half2 },
			{ "int2", AttributeType.Int2 },
			{ "double3", AttributeType.Double3 },
			{ "float3", AttributeType.Float3 },
			{ "half3", AttributeType.Half3 },
			{ "int3", AttributeType.Int3 },
			{ "double4", AttributeType.Double4 },
			{ "float4", AttributeType.Float4 },
			{ "half4", AttributeType.Half4 },
			{ "int4", AttributeType.Int4 },
			// Roles
			{ "point3d", AttributeType.Double3 },
			{ "point3f", AttributeType.Float3 },
			{ "point3h", AttributeType.Half3 },
			{ "normal3d", AttributeType.Double3 },
			{ "normal3f", AttributeType.Float3 },
			{ "normal3h", AttributeType.Half3 },
			{ "vector3d", AttributeType.Double3 },
			{ "vector3f", AttributeType.Float3 },
			{ "vector3h", AttributeType.Half3 },
			{ "color3d", AttributeType.Double3 },
			{ "color3f", AttributeType.Float3 },
			{ "color3h", AttributeType.Half3 },
			{ "color4d", AttributeType.Double4 },
			{ "color4f", AttributeType.Float4 },
			{ "color4h", AttributeType.Half4 },
			{ "frame4d", AttributeType.Matrix4d },
			{ "texCoord2d", AttributeType.Double2 },
			{ "texCoord2f", AttributeType.Float2 },
			{ "texCoord2h", AttributeType.Half2 },
			{ "texCoord3d", AttributeType.Double3 },
			{ "texCoord3f", AttributeType.Float3 },
			{ "texCoord3h", AttributeType.Half3 },
			{ "group", AttributeType.Opaque }
		};

	private static IReadOnlyDictionary<string, AttributeRole> _attributeTypeRoles =
		new Dictionary<string, AttributeRole>
		{
			{ "point3d", AttributeRole.Point },
			{ "point3f", AttributeRole.Point },
			{ "point3h", AttributeRole.Point },
			{ "normal3d", AttributeRole.Normal },
			{ "normal3f", AttributeRole.Normal },
			{ "normal3h", AttributeRole.Normal },
			{ "vector3d", AttributeRole.Vector },
			{ "vector3f", AttributeRole.Vector },
			{ "vector3h", AttributeRole.Vector },
			{ "color3d", AttributeRole.Color },
			{ "color3f", AttributeRole.Color },
			{ "color3h", AttributeRole.Color },
			{ "color4d", AttributeRole.Color },
			{ "color4f", AttributeRole.Color },
			{ "color4h", AttributeRole.Color },
			{ "frame4d", AttributeRole.Frame },
			{ "texCoord2d", AttributeRole.TexCoord },
			{ "texCoord2f", AttributeRole.TexCoord },
			{ "texCoord2h", AttributeRole.TexCoord },
			{ "texCoord3d", AttributeRole.TexCoord },
			{ "texCoord3f", AttributeRole.TexCoord },
			{ "texCoord3h", AttributeRole.TexCoord },
			{ "group", AttributeRole.Group }
		};

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

		var reader = new TokenReader( tokens );
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
			
			if ( !_attributeTypeNames.TryGetValue( typeName, out var attributeType ) ) { Log.Info( $"Unknown type: {typeName}" ); }
			_attributeTypeRoles.TryGetValue( typeName, out var attributeRole );

			var isArray = reader.Peek() is { Type: TokenType.BracketLeft }; 
			if ( isArray )
			{
				// Dequeue the square brackets.
				reader.Read();
				Assert.AreEqual( reader.Read()?.Type, TokenType.BracketRight, "Expected right square bracket in array definition." );
			}
			
			var attributeName = reader.ReadLabel();
			object attributeValue = null;
			
			// If there's a '=' here, try to read the value after it.
			if ( reader.Peek() is { Type: TokenType.OpBinaryAssign } )
			{
				reader.Read();
				attributeValue = isArray 
					? reader.ReadValueArray( attributeType ) 
					: reader.ReadValue( attributeType );
			}

			AddAttribute( attributeType, attributeRole, isArray, attributeName, attributeValue );
			if ( reader.Peek() is { Type: TokenType.ParenLeft } )
			{
				reader.Read();
				reader.SkipCurrentMetadata();
			}
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

		void AddAttribute( AttributeType type, AttributeRole role, bool isArray, string name, object value )
		{
			primStack.Peek().AddAttribute( type, role, isArray, name, value );
		}
	}
}
