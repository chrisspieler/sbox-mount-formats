using System;

namespace Duccsoft.Formats.Usd;

/// <summary>
/// Represents a prim description in an SdfLayer object.
/// </summary>
public class SdfPrimSpec : SdfSpec
{
	/// <summary>
	/// Create a new pseudoroot prim in the specified layer.
	/// </summary>
	internal SdfPrimSpec( SdfLayer layer ) : base( layer, SdfPath.AbsoluteRootPath(), SdfSpecType.SdfSpecTypePseudoRoot )
	{
		
		NameParent = this;
		Specifier = SdfSpecifier.SdfSpecifierDef;
		NameToken = "/";
	}

	internal SdfPrimSpec( SdfPrimSpec parent, string name, SdfSpecifier spec ) : base(parent.Layer, parent.Path.AppendElementToken( name ), SdfSpecType.SdfSpecTypePrim )
	{
		NameParent = parent;
		Specifier = spec;
		NameToken = name;
	}

	public string Name => NameToken.GetText();
	public TfToken NameToken { get; }
	public SdfPrimSpec NameRoot => Layer.PseudoRoot;
	public SdfSpecifier Specifier { get; }

	public SdfPrimSpec NameParent
	{
		get => _nameParent;
		init
		{
			_nameParent = value;
			// Avoid infinite loop in pseudo-root.
			if ( _nameParent == this )
				return;
			
			_nameParent.InsertNameChild( this, _nameParent.NameChildren.Count );
		}
	}
	private readonly SdfPrimSpec _nameParent;

	public IReadOnlyList<SdfPrimSpec> NameChildren => _nameChildren;
	private readonly List<SdfPrimSpec> _nameChildren = [];

	public bool InsertNameChild( SdfPrimSpec child, int index = -1 )
	{
		if ( index > _nameChildren.Count )
			return false;
		
		_nameChildren.Insert( Math.Max( 0, index ), child );
		return true;
	}

	public static SdfPrimSpec New( SdfLayer parentLayer, string name, SdfSpecifier spec, string typeName = "" )
	{
		return new SdfPrimSpec(
			parentLayer.PseudoRoot,
			name,
			spec
		);
	}

	public static SdfPrimSpec New( SdfPrimSpec parentPrim, string name, SdfSpecifier spec, string typeName = "" )
	{
		return new SdfPrimSpec(
			parentPrim,
			name,
			spec
		);
	}
}
