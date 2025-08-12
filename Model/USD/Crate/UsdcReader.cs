using System.IO;

namespace Duccsoft.Formats.Usd.Crate;

public class UsdcReader : IFileReader<UsdStage>
{
	public UsdStage ReadFromPath( string filePath ) => ReadFromBytes( File.ReadAllBytes( filePath ) );
	
	public UsdStage ReadFromBytes( byte[] bytes )
	{
		return new( [] );
	}
}
