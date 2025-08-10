namespace Duccsoft.Formats;

public struct DDS_HEADER
{
	public int				dwSize;
	public int				dwFlags;
	public int				dwHeight;
	public int				dwWidth;
	public int				dwPitchOrLinearSize;
	public int				dwDepth;
	public int				dwMipMapCount;
	// There are 44 bytes of padding here.
	public long				padding0;
	public long				padding1;
	public long				padding2;
	public long				padding3;
	public long				padding4;
	// End padding
	public DDS_PIXEL_FORMAT	ddspf;
	public int				dwCaps;
	public int				dwCaps2;
	public int				dwCaps3;
	public int				dwCaps4;
	public int				dwReserved2;
}
