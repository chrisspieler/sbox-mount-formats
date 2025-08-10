namespace Duccsoft.Formats;

public interface IDecompressor
{
	byte[] Decompress( byte[] compressed, ulong uncompressedSize );
}
