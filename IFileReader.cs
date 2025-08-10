namespace Duccsoft.Formats;

public interface IFileReader<out T>
{
	T ReadFromPath( string filePath );
	T ReadFromBytes( byte[] bytes );
}
