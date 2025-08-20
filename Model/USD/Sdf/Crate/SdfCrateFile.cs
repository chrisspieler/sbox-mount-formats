using System;
using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Crate;

public class SdfCrateFile : IFileReader<SdfLayer>
{
	public static readonly byte[] UsdcMagic = "PXR-USDC"u8.ToArray();

	public record TocSection( string Name, ulong Start, ulong End );

	public struct Field
	{
		public uint TokenIndex;
		public ulong ValueRep;
	}

	public struct Spec
	{
		public uint PathIndex;
		public uint FieldSetIndex;
		public SdfSpecType SpecType;
	}

	public TocSection[] Sections { get; private set; } = [];
	public TfToken[] Tokens { get; private set; } = [];
	public uint[] StringIndices { get; private set; } = [];
	public Field[] Fields { get; private set; } = [];
	public uint[] FieldSets { get; private set; } = [];
	public SdfPath[] Paths { get; private set; } = [];
	public Spec[] Specs { get; private set; } = [];

	public SdfLayer ReadFromPath( string filePath )
	{
		var bytes = File.ReadAllBytes( filePath );
		Log.Info( $"Read {bytes.Length.FormatBytes()} file: {filePath}" );
		return ReadFromBytes( bytes );
	}

	public SdfLayer ReadFromBytes( byte[] bytes )
	{
		var reader = new BinaryReader( new MemoryStream( bytes ) );
		Assert.True( reader.ReadBytes( 8 ).SequenceEqual( UsdcMagic ), "Invalid USDC magic" );

		var version = new Version( reader.ReadByte(), reader.ReadByte(), reader.ReadByte() );
		Log.Info( $"USDC version: {version.ToString()}" );

		// Skip version padding.
		reader.BaseStream.Position += 5;

		var tocOffset = reader.ReadUInt64();
		reader.BaseStream.Position = (long)tocOffset;

		Sections = new TocSection[reader.ReadUInt64()];
		for ( int i = 0; i < Sections.Length; i++ )
		{
			var section = ReadTocSection();
			Sections[i] = section;

			// Log.Info( $"Section #{i} \"{Sections[i].Name}\" from 0x{Sections[i].Start:X8} to 0x{Sections[i].End:X8}" );
		}

		foreach ( var section in Sections )
		{
			switch ( section.Name )
			{
				case "TOKENS":
					ReadTokens( section, reader );
					break;
				case "STRINGS":
					ReadStringIndices( section, reader );
					break;
				case "FIELDS":
					ReadFields( section, reader );
					break;
				case "FIELDSETS":
					ReadFieldSets( section, reader );
					// foreach ( var fieldSet in fieldSets )
					// {
					// 	Log.Info( $"Field set idx: {fieldSet}" );
					// }
					break;
				case "PATHS":
					ReadPaths( section, reader );
					// foreach ( var path in _paths )
					// {
					// 	Log.Info( path );
					// }
					break;
				case "SPECS":
					ReadSpecs( section, reader );
					// foreach ( var spec in _specs )
					// {
					// 	Log.Info( $"{spec.SpecType} path:{spec.PathIndex}, fieldSet:{spec.FieldSetIndex}" );
					// }
					break;
				default:
					Log.Info( $"Unrecognized section: {section.Name}" );
					break;
			}
		}

		Log.Info( $"Read {Tokens.Length} tokens, {StringIndices.Length} strings, {Fields.Length} fields, {FieldSets.Length} fieldSets, {Paths.Length} paths, {Specs.Length} specs" );

		return new SdfLayer();

		TocSection ReadTocSection() => new TocSection(
			Name: ReadNullTerminatedString( 16 ),
			Start: reader.ReadUInt64(),
			End: reader.ReadUInt64()
		);

		string ReadNullTerminatedString( int maxLength )
		{
			Span<byte> str = stackalloc byte[maxLength];
			reader.BaseStream.ReadExactly( str );
			var nullIndex = str.IndexOf( (byte)0x0 );
			var slice = nullIndex < 0 ? str : str[..nullIndex];
			return System.Text.Encoding.UTF8.GetString( slice );
		}
	}

	private void ReadTokens( TocSection tokenSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)tokenSection.Start;
		var numTokens = reader.ReadUInt64();
		var uncompressedSize = reader.ReadUInt64();
		var compressedSize = reader.ReadUInt64();

		var uncompressed = Compression.DecompressFromBuffer( reader, (int)compressedSize, (int)uncompressedSize );
		Assert.True( uncompressed[^1] == 0x0, "The byte of token data must be null" );

		var tokenStream = new MemoryStream( uncompressed );
		Tokens = new TfToken[numTokens];
		for ( ulong i = 0; i < numTokens; i++ )
		{
			Tokens[i] = tokenStream.ReadNullTerminatedString( tokenStream.Position );
		}

		// Log.Info( $"Read {_tokens.Length} tokens" );
	}

	private void ReadStringIndices( TocSection stringsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)stringsSection.Start;
		StringIndices = new uint[reader.ReadUInt64()];
		for ( int i = 0; i < StringIndices.Length; i++ )
		{
			StringIndices[i] = reader.ReadUInt32();
		}

		// Log.Info( $"Read {StringIndices.Length} string indices" );
	}

	private void ReadFields( TocSection fieldsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)fieldsSection.Start;

		var numFields = (int)reader.ReadUInt64();
		// Log.Info( $"0x{reader.BaseStream.Position:X8} numFields: {numFields}" );
		Fields = new Field[numFields];
		
		var indices = IntegerCoding<uint,int>.ReadCompressedInts( reader, numFields );
		var values = IntegerCoding<ulong,long>.ReadCompressedInts( reader, numFields );
		
		// Log.Info( $"Indices: {indices.Length}, values: {values.Length}" );
		for ( int i = 0; i < Fields.Length; i++ )
		{
			Fields[i] = new Field { TokenIndex = indices[i], ValueRep = values[i] };
			// Log.Info( $"field {Fields[i].TokenIndex} {Fields[i].ValueRep}" );
		}
	}

	private void ReadFieldSets( TocSection fieldSetSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)fieldSetSection.Start;

		var numFieldSets = (int)reader.ReadUInt64();
		FieldSets = IntegerCoding<uint, int>.ReadCompressedInts( reader, numFieldSets ).ToArray();
	}

	private void ReadPaths( TocSection pathsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)pathsSection.Start;

		var numPaths = (int)reader.ReadUInt64();
		// For some reason, the number of paths is stored a second time? Freak out if this assumption doesn't hold.
		Assert.AreEqual( numPaths, (int)reader.ReadUInt64(), "Second numPaths was different from first." );

		Paths = new SdfPath[numPaths];
		
		var pathIndices = IntegerCoding<uint, int>.ReadCompressedInts( reader, numPaths );
		var elementTokenIndices = IntegerCoding<int, int>.ReadCompressedInts( reader, numPaths );
		var jumps = IntegerCoding<int, int>.ReadCompressedInts( reader, numPaths );
		
		BuildPaths(
			pathIndices : pathIndices,
			elementTokenIndices: elementTokenIndices,
			jumps: jumps,
			curIndex: 0,
			parentPath: SdfPath.EmptyPath
		);
	}

	private void BuildPaths( Span<uint> pathIndices, Span<int> elementTokenIndices, Span<int> jumps, int curIndex, SdfPath parentPath )
	{
		bool hasChild;
		bool hasSibling;
		
		do
		{
			var thisIndex = curIndex++;
			var pathIndex = (int)pathIndices[thisIndex];

			if ( parentPath.IsEmpty() )
			{
				parentPath = SdfPath.AbsoluteRootPath();
				Paths[pathIndex] = parentPath;
			}
			else
			{
				var isPrimPropertyPath = elementTokenIndices[thisIndex] < 0;
				var tokenIndex = Math.Abs( elementTokenIndices[thisIndex] );
				var elemToken = Tokens[tokenIndex];
				Paths[pathIndex] = isPrimPropertyPath
					? parentPath.AppendProperty( elemToken )
					: parentPath.AppendElementToken( elemToken );
			}

			hasChild = jumps[thisIndex] > 0 || jumps[thisIndex] == -1;
			hasSibling = jumps[thisIndex] >= 0;
			
			if ( !hasChild )
				continue;

			if ( hasSibling )
			{
				var siblingIndex = thisIndex + jumps[thisIndex];
				BuildPaths( pathIndices, elementTokenIndices, jumps, siblingIndex, parentPath );
			}
			parentPath = Paths[pathIndex];
		} while ( hasChild || hasSibling );
	}

	private void ReadSpecs( TocSection specsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)specsSection.Start;

		var numSpecs = (int)reader.ReadUInt64();
		Specs = new Spec[numSpecs];
		
		// Log.Info( $"0x{reader.BaseStream.Position:X8} numSpecs {numSpecs}" );

		var pathIndices = IntegerCoding<uint, int>.ReadCompressedInts( reader, numSpecs );
		// Log.Info( $"0x{reader.BaseStream.Position:X8} read {numSpecs} pathIndices" );
		var fieldSetIndices = IntegerCoding<uint, int>.ReadCompressedInts( reader, numSpecs );
		// Log.Info( $"0x{reader.BaseStream.Position:X8} read {numSpecs} fieldSetIndices" );
		var specTypes = IntegerCoding<uint, int>.ReadCompressedInts( reader, numSpecs );
		// Log.Info( $"0x{reader.BaseStream.Position:X8} read {numSpecs} specTypes" );

		for ( int i = 0; i < numSpecs; i++ )
		{
			Specs[i] = new Spec
			{
				PathIndex = pathIndices[i],
				FieldSetIndex = fieldSetIndices[i],
				SpecType = (SdfSpecType)specTypes[i]
			};
		}
	}
}
