using System.IO;

namespace Duccsoft.Formats.Usd.Ascii;

public class UsdaReader : IFileReader<UsdStage>
{
	public UsdStage ReadFromPath( string filePath ) => ReadFromBytes( File.ReadAllBytes( filePath ) );
	public UsdStage ReadFromBytes( byte[] bytes )
	{
		var tokenizer = new UsdaTokenizer( bytes, enableValidation: true );
		var tokens = tokenizer.TokenizeAll();
		
		Log.Info( $"Read {tokens.Count} tokens from USDA file, {tokens.Count( t => t.Type == TokenType.Label )} labels, {tokens.Count( t => t.Type == TokenType.OpBinaryAssign)} assignments, {tokens.Count( t => t.Type == TokenType.LiteralInt)} ints, {tokens.Count(t => t.Type == TokenType.LiteralFloat)} floats,  {tokens.Count( t => t.Type == TokenType.LiteralString )} strings" );
		
		var parser = new UsdaParser( tokens, tokenizer.Lines );
		return parser.Parse();
	}
}
