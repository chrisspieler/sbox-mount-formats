using System;

namespace Duccsoft.Formats.Usd;

public class SdfPath
{
	public SdfPath()
	{
		
	}

	public SdfPath( string path )
	{
		_path = path;
	}

	// TODO: Remove this and use nodes instead of strings.
	private string _path;
	
	public static SdfPath EmptyPath => new();

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
	public SdfPath AppendElementToken( TfToken elementTok ) => new SdfPath( _path + "/" + elementTok );
	public SdfPath AppendProperty( TfToken propName ) => new SdfPath( _path + "." + propName );

	public string GetAsString() => _path;

	public override string ToString() => GetAsString();

	public bool IsEmpty() => this == EmptyPath;
}
