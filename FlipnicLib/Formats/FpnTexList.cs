namespace FlipnicLib.Formats;

public class FpnTexList
{
    private readonly TexEntry[] _entries;
    
    public FpnTexList(Stream stream)
    {
        var rawEntryCount = new byte[4];
        stream.ReadExactly(rawEntryCount, 0, 4);
        _entries = new TexEntry[BitConverter.ToInt32(rawEntryCount, 0)];
        stream.Position += 0xC;
        for (var i = 0; i < _entries.Length; i++)
        {
            var entryFile = new byte[0x10];
            stream.ReadExactly(entryFile, 0, entryFile.Length);
            var numbers = new byte[0x10];
            stream.ReadExactly(numbers, 0, numbers.Length);
            _entries[i] = new TexEntry
            {
                FileName = StaticUtils.GetString(entryFile),
                PositionX = BitConverter.ToInt32(numbers, 0),
                PositionY = BitConverter.ToInt32(numbers, 4),
                Width = BitConverter.ToInt32(numbers, 8),
                Height = BitConverter.ToInt32(numbers, 12),
            };
        }
    }

    public override string ToString()
    {
        var colHeaders = new[] { "Texture", "Offset/Dimensions" };
        var rows = _entries.Select(entry => (string[])[entry.FileName, $"{entry.PositionX}x{entry.PositionY}x{entry.Width}x{entry.Height}"]).ToList();
        return $"Texture list\nCount: {_entries.Length}\n\n" + StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
    }

    private struct TexEntry
    {
        public string FileName { get; init; }
        public int PositionX  { get; init; }
        public int PositionY { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
    }
}