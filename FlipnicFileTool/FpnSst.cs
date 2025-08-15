namespace FlipnicFileTool;

public class FpnSst
{
    private byte[] Data;
    private Dictionary<string, TocEntry> TableOfContents =  new();
    private int Count;
    
    public FpnSst(string filename)
    {
        Data = File.ReadAllBytes(filename);
        Count = StaticUtils.GetInt32(Data, 0x08);
        GenerateTOC(StaticUtils.GetInt32(Data, 0x0C));
    }

    public void GenerateMagicNumbers()
    {
        foreach (var entry in TableOfContents.Where(entry => entry.Key.EndsWith('N')))
        {
            Console.WriteLine("\n----------------------------");
            Console.WriteLine(entry.Key);
            Console.WriteLine("----------------------------");
            var subEntries = GetSubentries(entry.Value.Offset, entry.Value.EntrySize, entry.Value.Count);
            for (var i = 0; i < subEntries.Count; i++)
            {
                Console.WriteLine(i.ToString("X") + ": " + StaticUtils.GetString(subEntries[i]));
            }
        }
    }
    
    public void ListEntries()
    {
        string[] colHeaders = ["Name", "Offset", "Entry count", "Entry size"];
        var rows = TableOfContents.Select(entry => (string[]) [entry.Key, $"0x{entry.Value.Offset:X}", entry.Value.Count.ToString(), $"0x{entry.Value.EntrySize:X}"]).ToList();
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows));
    }

    private List<byte[]> GetSubentries(int offset, int entrySize, int count)
    {
        List<byte[]> subentries = [];
        for (var i = 0; i < count; i++)
        {
            subentries.Add(Data.Skip(offset+i*entrySize).Take(entrySize).ToArray());
        }
        return subentries;
    }

    private void GenerateTOC(int end)
    {
        for (var i = 0x10; i < end; i+=0x10)
        {
            var Name = StaticUtils.GetStringAt(Data, i);
            while (TableOfContents.ContainsKey(Name))
            {
                Name += "_";
            }

            if (Name.Length > 8)
            {
                Name =  Name[..8];
            }
            TableOfContents.Add(Name, new TocEntry
            {
                Count = StaticUtils.GetInt16(Data, i+8),
                EntrySize = StaticUtils.GetInt16(Data, i+10),
                Offset = StaticUtils.GetInt32(Data, i+0xC),
            });
        }
    }
}