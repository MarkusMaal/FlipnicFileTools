namespace FlipnicLib.Formats;

public class FpnFpd
{
    
    private string Label {get; set;}
    
    private int EntriesCount {get; set;}
    
    private Entry[] Entries {get; set;}
    
    public FpnFpd(string filename) : this(File.OpenRead(filename)) {}

    public FpnFpd(Stream fileStream)
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

    public float[] DrawPath()
    {
        int thickness = 50;
        List<float> f = new();
        var uv = 0f;
        for (var i = 1; i < EntriesCount; i+=1)
        {
            f.Add(uv);
            f.Add(uv);
            
            f.Add(Entries[i - 1].X / 4096f);
            f.Add(Entries[i - 1].Y / 4096f);
            f.Add(Entries[i - 1].Z / 4096f);

            for (var j = 0; j < 5; j++)
            {
                f.Add(uv);   
            }
            
            f.Add((Entries[i].X - thickness) / 4096f);
            f.Add((Entries[i].Y - thickness) / 4096f);
            f.Add((Entries[i].Z - thickness) / 4096f);
            

            for (var j = 0; j < 5; j++)
            {
                f.Add(uv);   
            }
            
            f.Add((Entries[i - 1].X - thickness) / 4096f);
            f.Add((Entries[i - 1].Y - thickness) / 4096f);
            f.Add((Entries[i - 1].Z - thickness) / 4096f);
            

            for (var j = 0; j < 5; j++)
            {
                f.Add(uv);   
            }
            
            f.Add(Entries[i - 1].X / 4096f);
            f.Add(Entries[i - 1].Y / 4096f);
            f.Add(Entries[i - 1].Z / 4096f);
            
            for (var j = 0; j < 5; j++)
            {
                f.Add(uv);   
            }
            f.Add((Entries[i].X) / 4096f);
            f.Add((Entries[i].Y) / 4096f);
            f.Add((Entries[i].Z) / 4096f);
            for (var j = 0; j < 5; j++)
            {
                f.Add(uv);   
            }
            
            f.Add((Entries[i].X - thickness) / 4096f);
            f.Add((Entries[i].Y - thickness) / 4096f);
            f.Add((Entries[i].Z - thickness) / 4096f);
            for (var j = 0; j < 3; j++)
            {
                f.Add(uv);   
            }

            if (i == 1)
            {
                uv += 0.1428571429f;
            }
        }
        return f.ToArray();
    }

    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["X", "Y", "Z", "W"];
        List<string[]> rows = [];
        rows.AddRange(Entries.Select(entry => (string[])[entry.X.ToString(), entry.Y.ToString(), entry.Z.ToString(), entry.W.ToString()]));
        return $"""
                Fixed Path Data

                Label: {Label}
                Points: {EntriesCount}

                Entries:
                {StaticUtils.GenerateTable(colHeaders, rows, asCsv)}
                """;
    }

    public override string ToString()
    {
        return ToString(StaticUtils.SimpleOutput);
    }
    
    private struct Entry
    {
        public short X { get; set; }
        public short Y { get; set; }
        public short Z { get; set; }
        public short W { get; set; }
    }
}