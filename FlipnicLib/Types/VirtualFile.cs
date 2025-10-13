namespace FlipnicLib.Types;

public class VirtualFile(string path, long offset, long length, long tocOffset, bool largeBuffer)
{
    public string Path { get; set; } = path;
    public long Offset { get; } = offset;
    public long Length { get; } = length;
    
    public long TocOffset { get; } = tocOffset;

    public bool LargeBuffer { get; } = largeBuffer;
    
    public string OffsetX => offset != -1 ? offset.ToString("X") : "N/A";
    public string LengthX => StaticUtils.GetFilesizeString(length);
}