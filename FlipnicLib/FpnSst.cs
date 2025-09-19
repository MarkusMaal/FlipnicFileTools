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
        return StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
    }

    public string GetStringById(string listName, int id)
    {
        foreach (var entry in TableOfContents.Where(entry => entry.Key.Equals(listName)))
        {
            var subEntries = GetSubentries(entry.Value.Offset, entry.Value.EntrySize, entry.Value.Count);
            var stop = false;
            if (id < 0)
            {
                id *= -1;
                id -= 1;
                stop = true;
            }
            try
            {
                return StaticUtils.GetString(subEntries[id]) + (stop ? ":NEG" : "");
            }
            catch (ArgumentOutOfRangeException)
            {
                return $"out of range, ID: {id:X}";
            }
        }

        return "(null)";
    }
    
    public string ListEntries()
    {
        string[] colHeaders = ["Name", "Offset", "Entry count", "Entry size"];
        var rows = TableOfContents.Select(entry => (string[]) [entry.Key, $"0x{entry.Value.Offset:X}", entry.Value.Count.ToString(), $"0x{entry.Value.EntrySize:X}"]).ToList();
        return StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
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
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput));
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

    public string GeneratePseudoCode()
    {
        try
        {
            StaticUtils.WindowWidth = Console.WindowWidth;
        } catch (IOException)
        {
            StaticUtils.WindowWidth = 200;
        }
        var o = "";
        var sOffset = TableOfContents["EVENT"].Offset;
        var eSize = TableOfContents["EVENT"].EntrySize;
        var eCount = TableOfContents["EVENT"].Count;
        for (var i = sOffset; i < sOffset + eSize * eCount; i += eSize)
        {
            o += new Event(_data.Skip(i).Take(eSize).ToArray()).GetPseudoCodeLine(this, i, StaticUtils.MsgFile != "" ? new FpnMsg(StaticUtils.MsgFile) : null);
        }
        return o;
    }

    public bool HasScoreRecord()
    {
        return TableOfContents.ContainsKey("RECORD");
    }
    
    public FpnSave GetSaveFromRecord()
    {
        var data = new byte[0x2780];
        var recordEntry = TableOfContents["RECORD"];
        var scoreData = new List<byte>();
        
        for (var i = 0; i < recordEntry.Count; i++)
        {
            var offset = recordEntry.Offset + (i*recordEntry.EntrySize);
            scoreData.AddRange(_data.Skip(offset).Take(recordEntry.EntrySize-4).ToArray());
            scoreData.Add(0);
            scoreData.Add(0);
            scoreData.Add(0);
            scoreData.Add(0);
            scoreData.AddRange(_data.Skip(offset+recordEntry.EntrySize-4).Take(4).ToArray());
        }

        for (var j = 0x60; j < scoreData.Count + 0x60; j++)
        {
            data[j] = scoreData[j-0x60];
        }
        return new FpnSave(data);
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