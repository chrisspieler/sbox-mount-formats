namespace Duccsoft.Formats.Usd;

public interface ISdfFileFormat
{
	bool CanRead( string file );
	IEnumerable<string> FileExtensions { get; }
	bool IsPackage { get; }
	string PrimaryFileExtension { get; }
	bool Read( SdfLayer layer, string resolvedPath, bool metadataOnly );
	bool SupportsEditing { get; }
	bool SupportsReading { get; }
	bool SupportsWriting { get; }
}
