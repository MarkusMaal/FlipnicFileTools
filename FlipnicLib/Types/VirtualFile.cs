namespace FlipnicLib.Types;

public class VirtualFile(string path, long offset, long length)
{
    public string Path { get; set; } = path;
    public long Offset { get; } = offset;
    public long Length { get; } = length;
    
    public string OffsetX { get; set; } = offset.ToString("X");
    public string LengthX { get; set; } = StaticUtils.GetFilesizeString(length);
}