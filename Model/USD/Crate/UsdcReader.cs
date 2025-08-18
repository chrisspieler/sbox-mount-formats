using System;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
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

		var tokens = Array.Empty<string>();
		var stringIndices = Array.Empty<uint>();
		var fields = Array.Empty<Field>();

		foreach ( var section in sections )
		{
			switch ( section.Name )
			{
				case "TOKENS":
					tokens = ReadTokens( section, reader );
					break;
				case "STRINGS":
					stringIndices = ReadStringIndices( section, reader );
					break;
				case "FIELDS":
					// fields = ReadFields( section, reader );
					break;
				case "FIELDSETS":
				case "PATHS":
				case "SPECS":
					break;
				default:
					Log.Info( $"Unrecognized section: {section.Name}" );
					break;
			}
		}

		Log.Info( $"Read {tokens.Length} tokens, {stringIndices.Length} strings, {fields.Length} fields" );

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

	private string[] ReadTokens( TocSection tokenSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)tokenSection.Start;
		var numTokens = reader.ReadUInt64();
		var uncompressedSize = reader.ReadUInt64();
		var compressedSize = reader.ReadUInt64();

		var uncompressed = DecompressFromBuffer( reader, (int)compressedSize, (int)uncompressedSize );
		Assert.True( uncompressed[^1] == 0x0, "The byte of token data must be null" );

		var tokenStream = new MemoryStream( uncompressed );
		var tokens = new string[numTokens];
		for ( ulong i = 0; i < numTokens; i++ )
		{
			tokens[i] = tokenStream.ReadNullTerminatedString( tokenStream.Position );
		}

		return tokens;
	}

	private uint[] ReadStringIndices( TocSection stringsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)stringsSection.Start;
		var indices = new uint[reader.ReadUInt64()];
		for ( int i = 0; i < indices.Length; i++ )
		{
			indices[i] = reader.ReadUInt32();
		}

		return indices;
	}

	private Field[] ReadFields( TocSection fieldsSection, BinaryReader reader )
	{
		reader.BaseStream.Position = (long)fieldsSection.Start;

		var numFields = (int)reader.ReadUInt64();
		Log.Info( $"0x{reader.BaseStream.Position:X8} numFields: {numFields}" );
		var fields = new Field[numFields];
		var indices = ReadCompressedInts<uint>( reader, numFields );
		var values = ReadCompressedInts<ulong>( reader, numFields );
		Log.Info( $"Indices: {indices.Length}, values: {values.Length}" );
		for ( int i = 0; i < fields.Length; i++ )
		{
			fields[i] = new Field { TokenIndex = indices[i], ValueRep = values[i] };
			Log.Info( $"field {fields[i].TokenIndex} {fields[i].ValueRep}" );
		}
		return fields;
	}

	private Span<T> ReadCompressedInts<T>( BinaryReader reader, int elementCount ) 
		where T : unmanaged
	{
		Log.Info( $"0x{reader.BaseStream.Position:X8} {nameof(ReadCompressedInts)}" );
		// Is the actual size of compressed data better than the worst case?
		var compressedSize = Math.Min( (int)reader.ReadUInt64(), GetMaxEncodedBufferSize<T>( elementCount ) );
		Log.Info( $"0x{reader.BaseStream.Position:X8} compressed size: {compressedSize}" );
		var uncompressedSize = Marshal.SizeOf<T>() * elementCount;
		Log.Info( $"uncompressed size: {uncompressedSize}" );
		byte[] bytes = DecompressFromBuffer( reader, compressedSize, uncompressedSize );
		return MemoryMarshal.Cast<byte, T>( bytes );
	}
	
	private byte[] DecompressFromBuffer( BinaryReader reader, int compressedSize, int outputLength )
	{
		// The chunks byte counts toward the compressed size, but is not part of the compressed data.
		compressedSize -= 1;
		var numChunks = reader.ReadByte();
		Log.Info( $"0x{reader.BaseStream.Position:X8} numChunks: {numChunks}" );
		Assert.True( numChunks == 0, $"There were actually {numChunks} chunks. Whoops!" );
		var output = new byte[outputLength];
		var compressed = reader.ReadBytes( compressedSize );
		var result = LZ4Codec.Decode( compressed, output );
		Log.Info( $"0x{reader.BaseStream.Position:X8} decompress {result} bytes" );
		return output[..result];
	}

	/// <summary>
	/// Gets the worst-case size of the compressed buffer for the given type and number of integers.
	/// </summary>
	private static int GetMaxEncodedBufferSize<TInt>( int numInts ) where TInt : unmanaged
	{
		var intSize = Marshal.SizeOf<TInt>();
		return numInts < 1 ? 0 : intSize + ((numInts * 2 + 7) / 8) + (numInts * intSize);
	}
}
