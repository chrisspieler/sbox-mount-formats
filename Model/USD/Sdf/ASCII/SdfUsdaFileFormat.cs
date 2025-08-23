namespace Duccsoft.Formats.Usd.Ascii;

public class SdfUsdaFileFormat : ISdfFileFormat
{
	public bool CanRead( string file ) => file.EndsWith( "." + PrimaryFileExtension );

	public IEnumerable<string> FileExtensions => [PrimaryFileExtension, "usd"];
	public bool IsPackage => false;
	public string PrimaryFileExtension => "usda";
	public bool Read( SdfLayer layer, string resolvedPath, bool metadataOnly )
	{
		throw new System.NotImplementedException();
	}

	public bool SupportsEditing => false;
	public bool SupportsReading => true;
	public bool SupportsWriting => false;
}
