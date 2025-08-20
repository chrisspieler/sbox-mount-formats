using Duccsoft.Formats.Usd.Crate;

namespace Duccsoft.Formats.Usd;

public class SdfLayer
{
	public SdfLayer()
	{
		PseudoRoot = new SdfPrimSpec( this );
	}

	public SdfPrimSpec PseudoRoot { get; }
	

	private Dictionary<SdfPath, SdfSpec> _directory;

	public static SdfLayer Load( SdfCrateFile crateFile )
	{
		var layer = new SdfLayer();

		foreach ( var spec in crateFile.Specs )
		{
			var path = crateFile.Paths[spec.PathIndex];
			var fieldSet = crateFile.FieldSets[spec.FieldSetIndex];
			Log.Info( $"{path} {spec.SpecType} {fieldSet}" );
			switch ( spec.SpecType )
			{
				case SdfSpecType.SdfSpecTypePrim:
					break;
				default:
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
