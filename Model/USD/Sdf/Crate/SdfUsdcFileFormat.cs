using System;
using System.IO;

namespace Duccsoft.Formats.Usd.Crate;

public class SdfUsdcFileFormat : ISdfFileFormat
{
	public bool CanRead( string file ) => file.EndsWith( "." + PrimaryFileExtension );
	
	public IEnumerable<string> FileExtensions => [ PrimaryFileExtension ];
	public bool IsPackage => false;
	public string PrimaryFileExtension => "usda";
	public bool Read( SdfLayer layer, string resolvedPath, bool metadataOnly )
	{
		ArgumentNullException.ThrowIfNull( layer, nameof(layer) );
		
		if ( !CanRead( resolvedPath ) )
			return false;
		
		var crateFile = SdfCrateFile.ReadFromPath( resolvedPath );
		
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
					var parent = layer.GetObjectAtPath( parentPath ) as SdfPrimSpec;
					var child = SdfPrimSpec.New( parent, path.GetName(), SdfSpecifier.SdfSpecifierDef );
					layer._directory[path] = child;
					break;
				default:
					Log.Info( $"Unhandled spec type: {spec.SpecType}" );
					break;
			}
		}

		return true;
	}

	public bool SupportsEditing => false;
	public bool SupportsReading => true;
	public bool SupportsWriting => false;
}
