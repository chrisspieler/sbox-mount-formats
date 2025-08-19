using System.IO;
using K4os.Compression.LZ4;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats.Usd.Crate;

public static class Compression
{
	public static byte[] DecompressFromBuffer( BinaryReader reader, int compressedSize, int outputLength )
	{
		// The chunks byte counts toward the compressed size, but is not part of the compressed data.
		compressedSize -= 1;
		var numChunks = reader.ReadByte();
		// Log.Info( $"0x{reader.BaseStream.Position:X8} numChunks: {numChunks}" );
		Assert.True( numChunks == 0, $"There were actually {numChunks} chunks. Whoops!" );
		var output = new byte[outputLength];
		var compressed = reader.ReadBytes( compressedSize );
		var result = LZ4Codec.Decode( compressed, output );
		// Log.Info( $"0x{reader.BaseStream.Position:X8} decompress {result} bytes" );
		return output[..result];
	}
}
