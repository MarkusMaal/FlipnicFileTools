using FlipnicFileTool.Types;

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
        string[] colHeaders = ["TOC name", "Index", "Value"];
        List<string[]> rows = [];
        foreach (var entry in TableOfContents.Where(entry => entry.Key.EndsWith('N') || entry.Key.EndsWith("NAME")))
        {
            var subEntries = GetSubentries(entry.Value.Offset, entry.Value.EntrySize, entry.Value.Count);
            rows.AddRange(subEntries.Select((t, i) => (string[]) [entry.Key, "0x" + i.ToString("X").PadLeft(2, '0'), StaticUtils.GetString(t)]));
        }
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows, rows.Select(row => row[2].Length + 1).Prepend(15).Max()));
    }
    
    public void ListEntries()
    {
        string[] colHeaders = ["Name", "Offset", "Entry count", "Entry size"];
        var rows = TableOfContents.Select(entry => (string[]) [entry.Key, $"0x{entry.Value.Offset:X}", entry.Value.Count.ToString(), $"0x{entry.Value.EntrySize:X}"]).ToList();
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows));
    }

    public void ShowGimmick(string name)
    {
        var tocEntry = TableOfContents[name];
        var gimmickData = Data.Skip(tocEntry.Offset).Take(tocEntry.EntrySize * tocEntry.Count).ToArray();
        List<Gimmick> gimmicks = [];
        for (var i = 0; i < tocEntry.Count; i++)
        {
            gimmicks.Add(new Gimmick(gimmickData.Skip(i * tocEntry.EntrySize).Take(tocEntry.EntrySize).ToArray()));
        }

        string[] colHeaders = ["Label", "Type", "Button", "Sound effect", "Flip. strength", "Knockback", "Bounciness"];
        List<string[]> rows = [];
        rows.AddRange(gimmicks.Select(entry => (string[]) [entry.Label, entry.Type.ToString(), entry.Button.ToString(), entry.SoundEffect.ToString(), StaticUtils.DotFloatString(entry.FlipperStrength), StaticUtils.DotFloatString(entry.Knockback), StaticUtils.DotFloatString(entry.Bounciness)]));
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows,
            rows.Select(row => row[0].Length).Prepend(15).Max()));
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