using System.Text;
using FlipnicLib.Types;

namespace FlipnicLib.Formats;

public class FpnSst : FormatBase
{
    private readonly byte[] _data;
    public readonly Dictionary<string, TocEntry> TableOfContents =  new();
    private int _count;
    
    public FpnSst(Stream stream)
    {
        _data = new byte[stream.Length];
        stream.ReadExactly(_data, 0, (int)stream.Length);
        _count = GetInt32(_data, 0x08);
        GenerateToc(GetInt32(_data, 0x0C));
    }

    /// <summary>
    /// Get the list of resources references by the SST file
    /// </summary>
    /// <returns>ASCII table containing TOC name, index and value</returns>
    public string GenerateMagicNumbers()
    {
        string[] colHeaders = ["TOC name", "Index", "Value"];
        List<string[]> rows = [];
        foreach (var entry in TableOfContents.Where(entry => entry.Key.EndsWith('N') || entry.Key.EndsWith("NAME")))
        {
            var subEntries = GetSubentries(entry.Value.Offset, entry.Value.EntrySize, entry.Value.Count);
            rows.AddRange(subEntries.Select((t, i) => (string[]) [entry.Key, "0x" + i.ToString("X").PadLeft(2, '0'), GetString(t)]));
        }
        return StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
    }

    private KeyValuePair<string, int> GetStageById(int id)
    {
        if (!TableOfContents.TryGetValue("STGINF", out var stgInfEntry)) return new KeyValuePair<string, int>("N/A", -1);
        for (var i = 0; i < stgInfEntry.Count; i++)
        {
            var absoluteOffset = i * stgInfEntry.EntrySize + stgInfEntry.Offset;
            var str = GetString(_data.Skip(absoluteOffset).Take(stgInfEntry.EntrySize)
                .ToArray());
            var cId = GetInt32(_data.Skip(absoluteOffset).Take(stgInfEntry.EntrySize).ToArray(), 0x20);
            if (i == id)
            {
                return new KeyValuePair<string, int>(str, cId);
            }
        }
        return new KeyValuePair<string, int>("N/A", -1);
    }

    /// <summary>
    /// Decode mission data stored in FNECMN.SST 
    /// </summary>
    /// <returns>Table containing the decoded values</returns>
    public string GetEvtInf()
    {
        if (!TableOfContents.TryGetValue("EVTINF", out _)) return "";
        string[] colHeaders = ["Offset", "Stage", "Texture", "Red", "Mission"];
        List<string[]> rows = [];
        
        var eventsEntry = TableOfContents["EVTINF"];
        var useJaMsg = File.Exists(StaticUtils.MsgFile);
        for (var i = 0; i < eventsEntry.Count; i += 1)
        {
            var absoluteOffset = eventsEntry.Offset + i * eventsEntry.EntrySize;
            var fullEntry = _data.Skip(absoluteOffset).Take(eventsEntry.EntrySize).ToArray();
            var stageIndex = GetInt32(fullEntry, 0);
            var stgInf = GetStageById(stageIndex);
            var isRed = fullEntry[4] == 0x01;
            var imgIdx = fullEntry[6];
            var stgIdx = stgInf.Value + 1;
            if (stgIdx > 4) stgIdx = 4 - (stgInf.Value % 4 + 1);
            if (stgIdx == 0) stgIdx = 2; // hack for Metallurgy B
            var pageRange = string.Join(",", Enumerable.Range(1, fullEntry[7]));
            var imgStr = "MI_ST" + (stgIdx) + "_M" + imgIdx.ToString().PadLeft(2, '0') + "_" + pageRange + ".TM2";
            var msgIdx = GetInt32(fullEntry, 8);
            var jaMsg = useJaMsg ? new FpnMsg(StaticUtils.MsgFile) : null;
            var msg = jaMsg != null ? (jaMsg?.Messages.Length > msgIdx ? jaMsg.Messages[msgIdx] : "JA.MSG:" + msgIdx) : "JA.MSG:" + msgIdx;
            rows.Add(["0x" + absoluteOffset.ToString("X"), stgInf.Key, imgStr, isRed ? "*" : "", msg]);
        }
        return (StaticUtils.SimpleOutput ? "" : "Missions:\n") + StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput) + (!useJaMsg ? "Note: Using placeholders for mission names, please import JA.MSG and reload to display actual names!\n" : "");
    }

    /// <summary>
    /// Get a resource name from the ID specified
    /// </summary>
    /// <param name="listName">The string list inside the SST file</param>
    /// <param name="id">ID you want the string from</param>
    /// <returns>String located at the index specified by id</returns>
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
                return GetString(subEntries[id]) + (stop ? ":NEG" : "");
            }
            catch (ArgumentOutOfRangeException)
            {
                return $"out of range, ID: {id:X}";
            }
        }

        return "(null)";
    }
    
    /// <summary>
    /// Display SST file TOC entries as table string
    /// </summary>
    /// <returns>ASCII table containing the TOC entries</returns>
    public string ListEntries()
    {
        string[] colHeaders = ["Name", "Offset", "Entry count", "Entry size"];
        var rows = TableOfContents.Select(entry => (string[]) [entry.Key, $"0x{entry.Value.Offset:X}", entry.Value.Count.ToString(), $"0x{entry.Value.EntrySize:X}"]).ToList();
        return StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
    }

    /// <summary>
    /// Display gimmick data as table string
    /// </summary>
    /// <param name="name">Name of the gimmick (from TOC, always starts with GMK)</param>
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
        rows.AddRange(gimmicks.Select(entry => (string[]) [entry.Label, entry.Type.ToString(), entry.Button.ToString(), entry.SoundEffect.ToString(), DotFloatString(entry.FlipperStrength), DotFloatString(entry.Knockback), DotFloatString(entry.Bounciness)]));
        Console.Write(StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput));
    }

    /// <summary>
    /// Get gimmick data
    /// </summary>
    /// <returns>Dictionary where each key references a TOC entry (starts with GMK)</returns>
    public Dictionary<string, Gimmick[]> GetGimmicks()
    {
        Dictionary<string, Gimmick[]> gimmicks = [];
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

    /// <summary>
    /// Allows you to make customizations to gimmicks
    /// </summary>
    /// <param name="gimmicks">The modified gimmicks dictionary (key: TOC entry label/value: gimmick array)</param>
    /// <returns>Entire SST file as a byte array, which includes the modifications</returns>
    public byte[] PatchGimmicks(Dictionary<string, Gimmick[]> gimmicks)
    {
        if (_data.Clone() is not byte[] patchedData) return [];
        foreach (var (key, gimmick) in gimmicks)
        {
            var tocEntry = TableOfContents[key];
            var startOffset = tocEntry.Offset;
            var currentOffset = startOffset;
            foreach (var subGimmick in gimmick)
            {
                var labelData = Encoding.ASCII.GetBytes(subGimmick.Label);
                for (var i = currentOffset; i < currentOffset + 0x20; i++)
                {
                    if (labelData.Length > 0)
                    {
                        patchedData[i] = labelData[0];
                        labelData = labelData.Skip(1).ToArray();
                        continue;
                    }

                    patchedData[i] = 0x00;
                }
                patchedData[currentOffset + 0x20] = (byte)subGimmick.Type;
                patchedData[currentOffset + 0x28] = (byte)(subGimmick.NoSpawn ? 0x01 : 0x00);
                patchedData[currentOffset + 0x2A] = (byte)(subGimmick.Invisible ? 0x01 : 0x00);
                for (var i = 0; i < 4; i++) patchedData[currentOffset + 0x4C + i] = BitConverter.GetBytes(subGimmick.Bounciness)[i];
                for (var i = 0; i < 4; i++) patchedData[currentOffset + 0x54 + i] = BitConverter.GetBytes(subGimmick.Knockback)[i];
                for (var i = 0; i < 4; i++) patchedData[currentOffset + 0x5C + i] = BitConverter.GetBytes(subGimmick.SoundEffect)[i];
                patchedData[currentOffset + 0x6C] = (byte)subGimmick.Button;
                patchedData[currentOffset + 0x6D] = subGimmick.AnalogRange;
                for (var i = 0; i < 4; i++) patchedData[currentOffset + 0x74 + i] = BitConverter.GetBytes(subGimmick.FlipperStrength)[i];
                
                currentOffset += 0x80;
            }
        }

        return patchedData;
    }

    /// <summary>
    /// Get raw data from the TOC entry
    /// </summary>
    /// <param name="offset">Offset of the entry</param>
    /// <param name="entrySize">Size of each subentry</param>
    /// <param name="count">Number of subentries</param>
    /// <returns>List containing raw data from each subentry</returns>
    private List<byte[]> GetSubentries(int offset, int entrySize, int count)
    {
        List<byte[]> subentries = [];
        for (var i = 0; i < count; i++)
        {
            subentries.Add(_data.Skip(offset+i*entrySize).Take(entrySize).ToArray());
        }
        return subentries;
    }

    /// <summary>
    /// Creates semi human-readable pseudocode from the EVENT entry 
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Check if the SST file has the RECORD entry in TOC
    /// </summary>
    public bool HasScoreRecord()
    {
        return TableOfContents.ContainsKey("RECORD");
    }
    
    /// <summary>
    /// Convert the RECORD entry to FpnSave object
    /// </summary>
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
            var name = GetStringAt(_data, i);
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
                Count = GetInt16(_data, i+8),
                EntrySize = GetInt16(_data, i+10),
                Offset = GetInt32(_data, i+0xC),
            });
        }
    }

    /// <summary>
    /// Gets the metadata about cameras used by various areas on a stage
    /// </summary>
    /// <param name="asCsv">Use simple output</param>
    /// <returns>Table containing the data</returns>
    public string GetCamData(bool asCsv = false)
    {
        if (TableOfContents.All(e => e.Key != "CAMD")) return "";
        string[] colHeaders = ["Area code", "Reference file", "Lock axes", "Anchored", "Stiffness"];
        List<CamData> cameras = [];
        foreach (var (_, tocEntry) in TableOfContents.Where(e => e.Key == "CAMD"))
        {
            var camData = _data.Skip(tocEntry.Offset).Take(tocEntry.EntrySize * tocEntry.Count).ToArray();
            for (var i = 0; i < tocEntry.Count; i++)
            {
                cameras.Add(new CamData(camData.Skip(i * tocEntry.EntrySize).Take(tocEntry.EntrySize).ToArray(), this));
            }
        }
        List<string[]> rows = [];
        rows.AddRange(cameras.Select(cam => (string[])[GetStringById("KUIDX", cam.CameraId), cam.CameraName, cam.GetAxisString(), (cam.AnchorToTarget ? "Yes" : "No"), cam.GetStiffnessXyz()]));
        
        return (StaticUtils.SimpleOutput ? "" : "\nCameras:\n") + StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }
}