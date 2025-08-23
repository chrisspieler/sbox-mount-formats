using System;
using System.IO;
using Duccsoft.Formats.Usd.Ascii;
using Duccsoft.Formats.Usd.Crate;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd;

using FileFormatArguments = System.Collections.Generic.Dictionary<string,string>;

public class SdfLayer
{
	private SdfLayer( ISdfFileFormat fileFormat, string identifier, string realPath = null, FileFormatArguments args = null )
	{
		// TODO: Assert not null? What's the real path?
		realPath ??= identifier;
		
		PseudoRoot = new SdfPrimSpec( this );
		_directory[PseudoRoot.Path] = PseudoRoot;
	}

	public ISdfFileFormat FileFormat { get; }
	public FileFormatArguments FileFormatArguments { get; }
	public SdfPrimSpec PseudoRoot { get; }


	// TODO: Make this private.
	public readonly Dictionary<SdfPath, SdfSpec> _directory = [];

	public static SdfLayer CreateNew( string identifier, FileFormatArguments args = null ) 
		=> CreateNew( null, identifier, args );

	public static SdfLayer CreateNew( ISdfFileFormat fileFormat, string identifier, FileFormatArguments args = null )
	{
		if ( !SplitIdentifier( identifier, out var path, out var idArgs ) )
			throw new ArgumentException( $"Unable to split identifier: {identifier}", nameof(identifier) );

		args ??= new FileFormatArguments();
		foreach ( var (key, value) in idArgs )
		{
			// TODO: Verify whether the args in the identifier should override the args in the method parameter. 
			args.TryAdd( key, value );
		}
		
		// If no file format was given, infer it from the identifier info.
		fileFormat ??= GetFileFormat( path, args );

		Assert.NotNull( fileFormat, $"Unable to find suitable file format for identifier: {identifier}" );

		// TODO: Look up the real path that the identifier path should have resolved to.
		return new SdfLayer( fileFormat, identifier, path, args );
	}

	private static ISdfFileFormat GetFileFormat( string path, FileFormatArguments args )
	{
		var extension = Path.GetExtension( path );
		// TODO: Create a static registry of ISdfFileFormat instances, and check the supported extensions of each.
		return extension switch
		{
			".usdc" => new SdfUsdcFileFormat(),
			".usda" => new SdfUsdaFileFormat(),
			_ => null
		};
	}

	// TODO: Actually create an identifier.
	public static string CreateIdentifier( string layerPath, FileFormatArguments args ) => layerPath;

	// TODO: Actually parse an identifier.
	public static bool SplitIdentifier( string identifier, out string layerPath, out FileFormatArguments args )
	{
		args = null;
		layerPath = identifier;
		return true;
	}

	public SdfSpec GetObjectAtPath( SdfPath path ) => _directory.GetValueOrDefault( path );

	public bool HasSpec( SdfPath path ) => _directory.TryGetValue( path, out var spec ) && spec is not null;
	public SdfSpecType GetSpecType( SdfPath path )
	{
		if ( !_directory.TryGetValue( path, out var spec ) )
			return SdfSpecType.SdfSpecTypeUnknown;

		return spec?.SpecType ?? SdfSpecType.SdfSpecTypeUnknown;
	}
}
