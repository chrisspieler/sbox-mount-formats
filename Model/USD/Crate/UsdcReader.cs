using System;
using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Crate;

public class UsdcReader : IFileReader<UsdStage>
{
	public static readonly byte[] UsdcMagic = "PXR-USDC"u8.ToArray();

	private record TocSection( string Name, long Start, long End );
	
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

		var tocOffset = reader.ReadInt64();
		reader.BaseStream.Position = tocOffset;

		var sections = new TocSection[reader.ReadUInt64()];
		for ( int i = 0; i < sections.Length; i++ )
		{
			sections[i] = ReadSection();
			Log.Info( $"Section #{i} \"{sections[i].Name}\" from 0x{sections[i].Start:X8} to 0x{sections[i].End:X8}" );
		}
		
		return new UsdStage( [] );

		TocSection ReadSection() => new TocSection(
			Name: System.Text.Encoding.UTF8.GetString( reader.ReadBytes( 16 ) ).Trim(),
			Start: reader.ReadInt64(),
			End: reader.ReadInt64()
		);
	}
}
