namespace FlipnicLib.Types;

public class TocEntry
{
    public int Offset { get; init; }
    public short Count { get; init; }
    public short EntrySize { get; init; }
}