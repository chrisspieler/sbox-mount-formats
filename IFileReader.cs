namespace Duccsoft.Formats;

public interface IFileReader<out T>
{
	T ReadFromPath( string filePath ) => ReadFromBytes( System.IO.File.ReadAllBytes( filePath ) );
	T ReadFromBytes( byte[] bytes );
}
