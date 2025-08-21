using FlipnicLib.Types;

namespace FlipnicLib;

public class FpnSst
{
    private readonly byte[] _data;
    public readonly Dictionary<string, TocEntry> TableOfContents =  new();
    private int _count;
    
    public FpnSst(Stream stream)
    {
        _data = new byte[stream.Length];
        stream.ReadExactly(_data, 0, (int)stream.Length);
        _count = StaticUtils.GetInt32(_data, 0x08);
        GenerateToc(StaticUtils.GetInt32(_data, 0x0C));
    }

    public string GenerateMagicNumbers()
    {
        string[] colHeaders = ["TOC name", "Index", "Value"];
        List<string[]> rows = [];
        foreach (var entry in TableOfContents.Where(entry => entry.Key.EndsWith('N') || entry.Key.EndsWith("NAME")))
        {
            var subEntries = GetSubentries(entry.Value.Offset, entry.Value.EntrySize, entry.Value.Count);
            rows.AddRange(subEntries.Select((t, i) => (string[]) [entry.Key, "0x" + i.ToString("X").PadLeft(2, '0'), StaticUtils.GetString(t)]));
        }
        return StaticUtils.GenerateTable(colHeaders, rows, rows.Select(row => row[2].Length + 1).Prepend(15).Max());
    }
    
    public string ListEntries()
    {
        string[] colHeaders = ["Name", "Offset", "Entry count", "Entry size"];
        var rows = TableOfContents.Select(entry => (string[]) [entry.Key, $"0x{entry.Value.Offset:X}", entry.Value.Count.ToString(), $"0x{entry.Value.EntrySize:X}"]).ToList();
        return StaticUtils.GenerateTable(colHeaders, rows);
    }

    public void ShowGimmick(string name)
    {
        var tocEntry = TableOfContents[name];
        var gimmickData = _data.Skip(tocEntry.Offset).Take(tocEntry.EntrySize * tocEntry.Count).ToArray();
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

    public Dictionary<string, Gimmick[]>? GetGimmicks()
    {
        Dictionary<string, Gimmick[]>? gimmicks = [];
        foreach (var (key, tocEntry) in TableOfContents)
        {
            if (!key.StartsWith("GMK")) continue;
            var gimmickData = _data.Skip(tocEntry.Offset).Take(tocEntry.EntrySize * tocEntry.Count).ToArray();
            List<Gimmick> gmk = [];
            for (var i = 0; i < tocEntry.Count; i++)
            {
                gmk.Add(new Gimmick(gimmickData.Skip(i * tocEntry.EntrySize).Take(tocEntry.EntrySize).ToArray()));
            }
            var g = gmk.ToArray();
            gimmicks.Add(key, g);
        }
        return gimmicks;
    }

    private List<byte[]> GetSubentries(int offset, int entrySize, int count)
    {
        List<byte[]> subentries = [];
        for (var i = 0; i < count; i++)
        {
            subentries.Add(_data.Skip(offset+i*entrySize).Take(entrySize).ToArray());
        }
        return subentries;
    }

    private void GenerateToc(int end)
    {
        for (var i = 0x10; i < end; i+=0x10)
        {
            var name = StaticUtils.GetStringAt(_data, i);
            while (TableOfContents.ContainsKey(name))
            {
                name += "_";
            }

            if (name.Length > 8)
            {
                name =  name[..8];
            }

            while (TableOfContents.ContainsKey(name))
            {
                name += "_";
            }
            TableOfContents.Add(name, new TocEntry
            {
                Count = StaticUtils.GetInt16(_data, i+8),
                EntrySize = StaticUtils.GetInt16(_data, i+10),
                Offset = StaticUtils.GetInt32(_data, i+0xC),
            });
        }
    }
}