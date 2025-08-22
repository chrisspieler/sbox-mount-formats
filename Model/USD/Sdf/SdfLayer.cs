using Duccsoft.Formats.Usd.Crate;

namespace Duccsoft.Formats.Usd;

public class SdfLayer
{
	public SdfLayer()
	{
		PseudoRoot = new SdfPrimSpec( this );
		Log.Info( $"Add PseudoRoot: {PseudoRoot.Path}" );
		_directory[PseudoRoot.Path] = PseudoRoot;
	}

	public SdfPrimSpec PseudoRoot { get; }


	private readonly Dictionary<SdfPath, SdfSpec> _directory = [];

	public static SdfLayer Load( string crateFilePath )
	{
		var crateFile = SdfCrateFile.ReadFromPath( crateFilePath );
		var layer = new SdfLayer();

		foreach ( var spec in crateFile.Specs )
		{
			var path = crateFile.Paths[spec.PathIndex];
			var fieldSet = crateFile.FieldSets[spec.FieldSetIndex];
			Log.Info( $"{path} {spec.SpecType} {fieldSet}" );
			switch ( spec.SpecType )
			{
				case SdfSpecType.SdfSpecTypePrim:
					var parentPath = path.GetParentPath();
					Log.Info( $"\"{path}\" parent path: \"{parentPath}\", path count: {layer._directory.Count}" );
					var parent = layer._directory[parentPath] as SdfPrimSpec;
					var child = SdfPrimSpec.New( parent, path.GetName(), SdfSpecifier.SdfSpecifierDef );
					layer._directory[path] = child;
					break;
				default:
					Log.Info( $"Unhandled spec type: {spec.SpecType}" );
					break;
			}
		}

		return layer;
	}

	public bool HasSpec( SdfPath path ) => _directory.TryGetValue( path, out var spec ) && spec is not null;
	public SdfSpecType GetSpecType( SdfPath path )
	{
		if ( !_directory.TryGetValue( path, out var spec ) )
			return SdfSpecType.SdfSpecTypeUnknown;

		return spec?.SpecType ?? SdfSpecType.SdfSpecTypeUnknown;
	}
}
