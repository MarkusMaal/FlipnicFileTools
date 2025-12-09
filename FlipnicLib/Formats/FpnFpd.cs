namespace FlipnicLib.Formats;

public class FpnFpd
{
    
    private string Label {get; set;}
    
    private int EntriesCount {get; set;}
    
    private Entry[] Entries {get; set;}
    
    public FpnFpd(string filename) : this(File.OpenRead(filename)) {}

    public FpnFpd(FileStream fileStream)
    {
        fileStream.Seek(0x10, SeekOrigin.Begin);
        var buffer = new byte[0x20];
        fileStream.ReadExactly(buffer, 0, 0x20);
        Label = StaticUtils.GetString(buffer);
        fileStream.Seek(0x30, SeekOrigin.Begin);
        buffer = new byte[0x4];
        fileStream.ReadExactly(buffer, 0, 0x4);
        EntriesCount = BitConverter.ToInt32(buffer, 0);
        fileStream.Seek(0x40, SeekOrigin.Begin);
        buffer = new byte[EntriesCount * 0x10 / 2];
        fileStream.ReadExactly(buffer, 0, EntriesCount * 0x8);
        Entries = new Entry[EntriesCount];
        for (var i = 0; i < EntriesCount; i++)
        {
            Entries[i] = new Entry
            {
                X = StaticUtils.GetInt16(buffer, i * 8),
                Y = StaticUtils.GetInt16(buffer, i * 8 + 2),
                Z = StaticUtils.GetInt16(buffer, i * 8 + 4),
                W = StaticUtils.GetInt16(buffer, i * 8 + 6),
            };
        }
    }

    public override string ToString()
    {
        string[] colHeaders = ["X", "Y", "Z", "W"];
        List<string[]> rows = [];
        rows.AddRange(Entries.Select(entry => (string[])[entry.X.ToString(), entry.Y.ToString(), entry.Z.ToString(), entry.W.ToString()]));
        return $"""
                Fixed Path Data

                Label: {Label}
                Entries count: {EntriesCount}

                Entries:
                {StaticUtils.GenerateTable(colHeaders, rows, false)}
                """;
    }
    
    private struct Entry
    {
        public short X { get; set; }
        public short Y { get; set; }
        public short Z { get; set; }
        public short W { get; set; }
    }
}