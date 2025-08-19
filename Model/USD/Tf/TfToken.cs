using System;

namespace Duccsoft.Formats.Usd;

public record TfToken : IEquatable<TfToken>
{
	public TfToken() : this( string.Empty ) { }
	public TfToken( string s )
	{
		var strHash = s.GetHashCode();

		// If this token was already registered, use the existing hash.
		if ( TokenRegistry.TryGetValue( strHash, out var existingToken ) )
		{
			_hash = existingToken._hash;
			return;
		}
		
		// Add this string to the token registry as a new token.
		// TODO: Make this threadsafe.
		{
			TokenRegistry[_hash] = this;
			GlobalStrings.Add( s );
			_hash = GlobalStrings.Count - 1;
		}
	}
	
	private static readonly Dictionary<int, TfToken> TokenRegistry = [];
	private static readonly List<string> GlobalStrings = [];
	

	public int Hash() => _hash;
	private readonly int _hash;
	public override int GetHashCode() => _hash;

	public string GetText() => GlobalStrings[_hash];
	public override string ToString() => GlobalStrings[_hash];


	public static TfToken Find( string s )
	{
		var strHash = s.GetHashCode();
		return TokenRegistry.GetValueOrDefault( strHash );
	}

	public static bool operator ==( TfToken lhs, string s ) => lhs?.GetText() == s;
	public static bool operator !=( TfToken lhs, string s ) => !(lhs == s);
	public static implicit operator TfToken( string s ) => new ( s );
}
