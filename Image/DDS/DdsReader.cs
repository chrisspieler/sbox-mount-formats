using System.IO;
using Sandbox.Diagnostics;

namespace Duccsoft.Formats;

public class DdsReader : IFileReader<DdsFile>
{
	public DdsFile ReadFromBytes( byte[] bytes )
	{
		using var reader = new BinaryReader( new MemoryStream( bytes ) );
		
		var magic = reader.ReadInt32();
		Assert.AreEqual( magic, DdsMagic.HeaderMagic, $"Invalid DDS magic: 0x{magic:X8}");
		var header = reader.Read<DDS_HEADER>();
		
		DDS_HEADER_DXT10? dxt10 = null;
		if ( header.ddspf.dwFourCc == DdsMagic.FourCcDx10 )
		{
			dxt10 = reader.Read<DDS_HEADER_DXT10>();
		}

		reader.BaseStream.Position = dxt10 is null ? 128 : 148;
		var data = reader.ReadRemaining();
		return new DdsFile( header, dxt10, data );
	}
}
