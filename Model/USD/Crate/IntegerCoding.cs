using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Duccsoft.Formats.Usd.Crate;

public static class IntegerCoding<TInt, TSInt>
	where TInt : unmanaged, 
		IBinaryInteger<TInt>
	where TSInt : unmanaged, 
		IBinaryInteger<TSInt>, 
		ISignedNumber<TSInt>, 
		IAdditionOperators<TSInt, TSInt, TSInt>
{
	/// <summary>
	/// Gets the worst-case size of the compressed buffer for the given type and number of integers.
	/// </summary>
	public static int GetMaxEncodedBufferSize( int numInts )
	{
		var intSize = Marshal.SizeOf<TInt>();
		return numInts < 1 ? 0 : intSize + ((numInts * 2 + 7) / 8) + (numInts * intSize);
	}
	
	public static Span<TInt> ReadCompressedInts( BinaryReader reader, int elementCount )
	{
		Log.Info( $"0x{reader.BaseStream.Position:X8} {nameof(ReadCompressedInts)}" );
		// Is the actual size of compressed data better than the worst case?
		var compressedSize = Math.Min( (int)reader.ReadUInt64(), GetMaxEncodedBufferSize( elementCount ) );
		Log.Info( $"0x{reader.BaseStream.Position:X8} compressed size: {compressedSize}" );
		var uncompressedSize = Marshal.SizeOf<TInt>() * elementCount;
		Log.Info( $"uncompressed size: {uncompressedSize}" );
		byte[] bytes = Compression.DecompressFromBuffer( reader, compressedSize, uncompressedSize );
		return DecodeInts( bytes, elementCount );
	}
	public static TInt[] DecodeInts( byte[] data, int numInts )
	{
		var outputBytes = new byte[Marshal.SizeOf<TInt>() * numInts];

		using var codeReader = new BinaryReader( new MemoryStream( data ) );
		TSInt commonValue = codeReader.Read<TSInt>();
		
		using var valueReader = new BinaryReader( new MemoryStream( data ) );
		valueReader.BaseStream.Position = (numInts * 2 + 7) / 8;
		
		using var intWriter = new BinaryWriter( new MemoryStream( outputBytes ) );

		var prevVal = TSInt.AdditiveIdentity;
		var intsLeft = numInts;
		while ( intsLeft >= 4 )
		{
			DecodeNHelper( 4, codeReader, valueReader, commonValue, ref prevVal, intWriter );
			intsLeft -= 4;
		}

		switch ( intsLeft )
		{
			case 1:
				DecodeNHelper( 1, codeReader, valueReader, commonValue, ref prevVal, intWriter );
				break;
			case 2:
				DecodeNHelper( 2, codeReader, valueReader, commonValue, ref prevVal, intWriter );
				break;
			case 3:
				DecodeNHelper( 3, codeReader, valueReader, commonValue, ref prevVal, intWriter );
				break;
			default:
				break;
		}

		return MemoryMarshal.Cast<byte, TInt>( outputBytes ).ToArray();
	}

	private static void DecodeNHelper( int shift, BinaryReader codeReader, BinaryReader valueReader, TSInt commonValue, ref TSInt prevVal, BinaryWriter output )
	{
		byte codeByte = codeReader.ReadByte();
		
		Span<byte> outBytes = stackalloc byte[Marshal.SizeOf<TInt>()];
		
		for ( int i = 0; i < shift; i++ )
		{
			var code = (codeByte >> (2 * i)) & 3;
			switch ( code )
			{
				// Small
				case 1:
					prevVal += TSInt.CreateChecked( valueReader.ReadSByte() );
					break;
				// Medium
				case 2:
					prevVal += TSInt.CreateChecked( valueReader.ReadInt16() );
					break;
				// Large
				case 3:
					prevVal += TSInt.CreateChecked( valueReader.ReadInt32() );
					break;
				// Common
				default:
					prevVal += commonValue;
					break;
			}

			TInt.CreateChecked( prevVal ).WriteLittleEndian( outBytes );
			output.Write( outBytes );
		}
	}
}
