using System.IO;
using System.Runtime.InteropServices;

namespace Duccsoft;

public static class BinaryReaderExtensions
{
	public static T Read<T>( this BinaryReader reader ) where T : unmanaged
	{
		var size = Marshal.SizeOf<T>();
		var bytes = reader.ReadBytes( size );
		return MemoryMarshal.Read<T>( bytes );
	}
	
	public static byte[] ReadRemaining( this BinaryReader reader )
	{
		var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
		return reader.ReadBytes( (int)remaining );
	}
}
