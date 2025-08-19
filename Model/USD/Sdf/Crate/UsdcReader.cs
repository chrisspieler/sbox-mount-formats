using System;
using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Crate;

public class UsdcReader : IFileReader<UsdStage>
{
	public static readonly byte[] UsdcMagic = "PXR-USDC"u8.ToArray();

	private record TocSection( string Name, ulong Start, ulong End );

	private struct Field
	{
		public uint TokenIndex;
		public ulong ValueRep;
	}

	private SdfPath[] _paths = [];
	private TfToken[] _tokens = [];

	public UsdStage ReadFromPath( string filePath )
	{
		var bytes = File.ReadAllBytes( filePath );
		Log.Info( $"Read {bytes.Length.FormatBytes()} file: {filePath}" );
		return ReadFromBytes( bytes );
	}

	public UsdStage ReadFromBytes( byte[] bytes )
	{
		var reader = new BinaryReader( new MemoryStream( bytes ) );
		Assert.True( reader.ReadBytes( 8 ).SequenceEqual( UsdcMagic ), "Invalid USDC magic" );

		var version = new Version( reader.ReadByte(), reader.ReadByte(), reader.ReadByte() );
		Log.Info( $"USDC version: {version.ToString()}" );

		// Skip version padding.
		reader.BaseStream.Position += 5;

		var tocOffset = reader.ReadUInt64();
		reader.BaseStream.Position = (long)tocOffset;

		var sections = new TocSection[reader.ReadUInt64()];
		for ( int i = 0; i < sections.Length; i++ )
		{
			var section = ReadTocSection();
			sections[i] = section;

			Log.Info( $"Section #{i} \"{sections[i].Name}\" from 0x{sections[i].Start:X8} to 0x{sections[i].End:X8}" );
		}
		
		var stringIndices = Array.Empty<uint>();
		var fields = Array.Empty<Field>();
		Span<uint> fieldSets = [];

		foreach ( var section in sections )
		{
			switch ( section.Name )
			{
				case "TOKENS":
					ReadTokens( section, reader );
					break;
				case "STRINGS":
					stringIndices = ReadStringIndices( section, reader );
					break;
				case "FIELDS":
					fields = ReadFields( section, reader );
					break;
				case "FIELDSETS":
					fieldSets = ReadFieldSets( section, reader );
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
					
					break;
				default:
					Log.Info( $"Unrecognized section: {section.Name}" );
					break;
			}
		}

		Log.Info( $"Read {_tokens.Length} tokens, {stringIndices.Length} strings, {fields.Length} fields, {fieldSets.Length} fieldSets, {_paths.Length} paths" );

		return new UsdStage( [] );

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
		_tokens = new TfToken[numTokens];
		for ( ulong i = 0; i < numTokens; i++ )
		{
			_tokens[i] = tokenStream.ReadNullTerminatedString( tokenStream.Position );
		}

		Log.Info( $"Read {_tokens.Length} tokens" );
	}

	private uint[] ReadStringIndices( TocSection stringsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)stringsSection.Start;
		var indices = new uint[reader.ReadUInt64()];
		for ( int i = 0; i < indices.Length; i++ )
		{
			indices[i] = reader.ReadUInt32();
		}

		Log.Info( $"Read {indices.Length} string indices" );
		return indices;
	}

	private Field[] ReadFields( TocSection fieldsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)fieldsSection.Start;

		var numFields = (int)reader.ReadUInt64();
		// Log.Info( $"0x{reader.BaseStream.Position:X8} numFields: {numFields}" );
		var fields = new Field[numFields];
		
		var indices = IntegerCoding<uint,int>.ReadCompressedInts( reader, numFields );
		var values = IntegerCoding<ulong,long>.ReadCompressedInts( reader, numFields );
		
		Log.Info( $"Indices: {indices.Length}, values: {values.Length}" );
		for ( int i = 0; i < fields.Length; i++ )
		{
			fields[i] = new Field { TokenIndex = indices[i], ValueRep = values[i] };
			// Log.Info( $"field {fields[i].TokenIndex} {fields[i].ValueRep}" );
		}
		return fields;
	}

	private Span<uint> ReadFieldSets( TocSection fieldSetSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)fieldSetSection.Start;

		var numFieldSets = (int)reader.ReadUInt64();
		return IntegerCoding<uint, int>.ReadCompressedInts( reader, numFieldSets );
	}

	private void ReadPaths( TocSection pathsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)pathsSection.Start;

		var numPaths = (int)reader.ReadUInt64();
		// For some reason, the number of paths is stored a second time? Freak out if this assumption doesn't hold.
		Assert.AreEqual( numPaths, (int)reader.ReadUInt64(), "Second numPaths was different from first." );

		_paths = new SdfPath[numPaths];
		
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
				_paths[pathIndex] = parentPath;
			}
			else
			{
				var isPrimPropertyPath = elementTokenIndices[thisIndex] < 0;
				var tokenIndex = Math.Abs( elementTokenIndices[thisIndex] );
				var elemToken = _tokens[tokenIndex];
				_paths[pathIndex] = isPrimPropertyPath
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
			parentPath = _paths[pathIndex];
		} while ( hasChild || hasSibling );
	}
}
