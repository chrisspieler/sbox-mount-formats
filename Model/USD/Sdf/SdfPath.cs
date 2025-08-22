using System;

namespace Duccsoft.Formats.Usd;

public class SdfPath : IEquatable<SdfPath>
{
	public SdfPath( string path )
	{
		_path = path;
	}

	// TODO: Remove this and use nodes instead of strings.
	private readonly string _path;
	
	public static SdfPath EmptyPath => new( string.Empty );

	public override int GetHashCode()
	{
		return (_path != null ? _path.GetHashCode() : 0);
	}

	public static bool IsValidIdentifier( string name )
	{
		if ( name.Length < 1 )
			return false;

		// Cannot start with a number.
		if ( name[0] - '0' < 10 )
			return false;

		foreach (var c in name)
		{
			var isLetter = c - 'A' < 26 || c - 'a' < 26;
			var isNumber = c - '0' < 10;
			var isUnderScore = c == '_';
			// A valid identifier may contain only letters, numbers, and underscores.
			if ( !isLetter && !isNumber && !isUnderScore )
				return false;
		}
		return true;
	}
	
	public static bool IsValidNamespacedIdentifier( string name ) => IsValidIdentifier( name );
	public static bool IsValidPathString( string pathString, out string errorMsg )
	{
		errorMsg = string.Empty;
		return true;
	}
	
	public static SdfPath AbsoluteRootPath() => new SdfPath( "/" );
	public SdfPath AppendElementToken( TfToken elementTok ) => new SdfPath( _path?.TrimEnd( '/' ) + "/" + elementTok );
	public SdfPath AppendProperty( TfToken propName ) => new SdfPath( _path?.TrimEnd( '/' ) + "." + propName );

	public SdfPath GetParentPath()
	{
		var separatorIndex = GetLastSeparatorIndex();
		return separatorIndex < 1 
			? AbsoluteRootPath() 
			: new SdfPath( _path[..separatorIndex] );
	}

	public string GetName()
	{
		var separatorIndex = GetLastSeparatorIndex();
		return separatorIndex < 0
			// If there's no separator, this whole path *is* the name.
			? _path 
			// Capture everything after the last separator character we found.
			: _path[(separatorIndex + 1)..];
	}

	/// <summary>
	/// Returns the index of the last separator character in this path, or -1 if no separator character was found.
	/// If the last separator never occurred or occurred at the start of this string, then the parent of this spec is
	/// the pseudo-root, and this is a root prim.
	/// </summary>
	private int GetLastSeparatorIndex()
	{
		if ( _path == "/" )
			return 0;
		
		// Don't check the last character, because if a separator occurs on the last character, it's not actually
		// separating anything. There's no local path to extract on the right side of that separator.
		var numChars = _path.Length - 1;
		var lastIndex = -1;
		for ( int i = 0; i < numChars; i++ )
		{
			// Assumes a prim path
			// TODO: Handle properties, relations, etc.
			if ( _path[i] is not '/' )
				continue;
			
			lastIndex = i;
		}
		
		return lastIndex;
	}

	public string GetAsString() => _path;

	public override string ToString() => GetAsString();

	public bool IsEmpty() => Equals( EmptyPath );

	public static bool operator ==( SdfPath a, SdfPath b ) => a?.Equals( b ) == true;
	public static bool operator !=( SdfPath a, SdfPath b ) => !(a == b);

	public bool Equals(SdfPath other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return _path == other._path;
	}

	public override bool Equals(object obj)
	{
		if (obj is null)
			return false;

		if (ReferenceEquals(this, obj))
			return true;

		if (obj.GetType() != GetType())
			return false;

		return Equals((SdfPath)obj);
	}
}
