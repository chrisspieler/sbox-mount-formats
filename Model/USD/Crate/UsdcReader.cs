using System;
using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Crate;

public class UsdcReader : IFileReader<UsdStage>
{
	public static readonly byte[] UsdcMagic = "PXR-USDC"u8.ToArray();

	private record TocSection( string Name, ulong Start, ulong End );
	
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

		foreach ( var section in sections )
		{
			switch ( section.Name )
			{
				case "TOKENS":
					reader.BaseStream.Position = (long)section.Start;
					var numTokens = reader.ReadUInt64();
					var uncompressedSize = reader.ReadUInt64();
					var compressedSize = reader.ReadUInt64();
					Log.Info( $"Found TOKENS section. numTokens: {numTokens}, uncompressedSize: {uncompressedSize}, compressedSize: {compressedSize}" );
					// TODO: Decompress the LZ4-compressed integer array in the token section.
					break;
				case "STRINGS":
				case "FIELDS":
				case "FIELDSETS":
				case "PATHS":
				case "SPECS":
					break;
				default:
					Log.Info( $"Unrecognized section: {section.Name}" );
					break;
			}
		}
		
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
}
