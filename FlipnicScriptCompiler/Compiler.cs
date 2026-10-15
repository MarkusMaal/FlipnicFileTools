using System.Text;

namespace FlipnicScriptCompiler;

public class Compiler
{

    private readonly Dictionary<string, List<byte[]>> _sstSection = new(); 
    private readonly Dictionary<string, int> _sstSizes = new();

    private enum DataTypes
    {
        Float,
        Byte,
        Int,
        Short,
        Long,
        String,
        WString,
        SFmvType,
        SFadeInType,
        SFadeOutType,
        Dummy,
        Padding
    }
    
    public (string res, int line) CompileAsStream(string[] input, Stream output)
    {
        var sect = "???";
        foreach (var (i, l) in input.Index())
        {
            if (l == "") continue;
            if (l.StartsWith("//")) continue;
            if (l.StartsWith('#'))
            {
                sect = l[1..];
                _sstSection[sect] = [];
                Console.WriteLine($"Parsing {sect}");
                continue;
            }

            var buffer = new byte[0x20];
            byte[] idx;

            if (sect.StartsWith("GMK"))
            {
                var gmk = new Gimmick(l);
                buffer = gmk.ToBytes();
                goto endSwitch;
            }

            string[] lSplit;
            switch (sect)
            {
                case "SKYN":
                case "CAMN":
                case "LIGHTN":
                case "PATHN":
                case "KUIDX":
                case "BOSSN":
                case "STGNAME":
                    buffer = CsvToBytes(l, sect == "BALLN" ? [(DataTypes.String, 0x20), (DataTypes.String, 0x20)] : [(DataTypes.String, 0x20)], sect);
                    break;
                case "BALLN":
                case "VABN":
                case "INTN":
                    buffer = CsvToBytes(l, l.Split(',').Length > 2
                        ? [(DataTypes.String, 0x20), (DataTypes.String, 0x20), (DataTypes.Int, 1)]
                        : [(DataTypes.String, 0x20), (DataTypes.String, 0x20)], sect);
                    break;
                case "SEQN":
                case "EFFN":
                case "FNTN":
                case "STGINF":
                case "BOSS_REF":
                    lSplit = l.Split(',');
                    buffer = new byte[lSplit[1].EndsWith('L') ? 0x28 : 0x24];
                    foreach (var (j, b) in Encoding.UTF8.GetBytes(lSplit[0]).Index())
                        buffer[j] = b;
                    idx = lSplit[1].EndsWith('L') ? BitConverter.GetBytes(long.Parse(lSplit[1][..^1])) : BitConverter.GetBytes(int.Parse(lSplit[1]));
                    for (var j = 0; j < buffer.Length - 0x20; j++)
                        buffer[0x20 + j] = idx[j];
                    break;
                case "BALLD":
                    buffer = CsvToBytes(l, [(DataTypes.Float, 1)], sect);
                    break;
                case "LNEWARP":
                case "LNEWARP_":
                case "KUOFS":
                    buffer = CsvToBytes(l, [(DataTypes.Short, l.Split(',').Length)], sect);
                    break;
                case "KUINF":
                    buffer = CsvToBytes(l, [(DataTypes.Short, 0x10), (DataTypes.Float, 4), (DataTypes.Short, 6), (DataTypes.Byte, 12)], sect);
                    break;
                case "EVTINF":
                    buffer = CsvToBytes(l,
                    [
                        (DataTypes.Int, 1), (DataTypes.Byte, 1), (DataTypes.Padding, 1), (DataTypes.Byte, 2),
                        (DataTypes.Int, 1)
                    ], sect);
                    break;
                case "RECORD":
                    buffer = CsvToBytes(l, [(DataTypes.Long, 1), (DataTypes.Int, 2), (DataTypes.String, 0x20), (DataTypes.Int, 1)], sect);
                    break;
                case "CAMD":
                    lSplit = l.Split(',');
                    var stiffy = lSplit[9].Split('/');
                    buffer =
                    [
                        .. lSplit.Take(5).SelectMany(p => BitConverter.GetBytes(int.Parse(p))).ToArray(),
                        .. BitConverter.GetBytes(0),
                        .. BitConverter.GetBytes((int)GenerateCamDByte(lSplit[5], lSplit[6])),
                        .. BitConverter.GetBytes(_sstSection["CAMN"].FindIndex(p => lSplit[7].TrimEnd('\0') == Decompiler.GetString(p))),
                        .. BitConverter.GetBytes(int.Parse(lSplit[8])),
                        .. stiffy.SelectMany(p => BitConverter.GetBytes(float.Parse(p))).ToArray()
                    ];
                    break;
                case "CAMLD":
                    buffer = CsvToBytes(l, [(DataTypes.Int, 3), (DataTypes.Float, 2), (DataTypes.Int, 5)], sect);
                    break;
                case "IPUN":
                    buffer = CsvToBytes(l, [(DataTypes.String, 0x20), (DataTypes.String, 0x20), (DataTypes.SFmvType, 1), (DataTypes.Int, 1), (DataTypes.SFadeInType, 1), (DataTypes.Int, 1), (DataTypes.SFadeOutType, 1)], sect);
                    break;
                case "CAMKUD":
                    buffer = CsvToBytes(l + ",255", [(DataTypes.Float, 2), (DataTypes.Byte, 1), (DataTypes.Dummy, 1), (DataTypes.Byte, l.Split(',').Length - 1)], sect);
                    break;
                case "REBIRTH":
                case "REBIRTH_":
                case "REBIRTH+":
                    buffer = CsvToBytes(l, [(DataTypes.Short, 3)], sect);
                    break;
                case "SGKTBL":
                    buffer = CsvToBytes(l, [(DataTypes.Short, 2)], sect);
                    break;
                case "SGKIDX":
                case "EVTIDX":
                    buffer = BitConverter.GetBytes(int.Parse(l));
                    break;
                case "FLIESFPB":
                    buffer = Convert.FromBase64String(l[8..][..(l[8..].Length - 1)]);
                    break;
                case "DRAWD":
                    buffer = CsvToBytes(l, [(DataTypes.Float, 1), (DataTypes.Int, 1)], sect);
                    break;
                case "MTTBL":
                case "PTTBL":
                case "MTTBLDEF":
                case "EVTBL":
                    buffer = [.. l.Split(',').Select(byte.Parse)];
                    break;
                case "SETBL":
                    buffer = CsvToBytes(l, [(DataTypes.Byte, 8), (DataTypes.Int, 2)], sect);
                    break;
                case "EVENT":
                    buffer = Event.Parse(l);
                    break;
                case "WALLD":
                case "PLIGHT":
                    lSplit = l.Split(',');
                    buffer = CsvToBytes(l, [(DataTypes.Float, lSplit.Length)], sect);
                    break;
                case "BOSS_TEX":
                    buffer = CsvToBytes(l, [(DataTypes.String, 0x20), (DataTypes.Int, 2), (DataTypes.Float, 2)], sect);
                    break;
                case "BOSS_CAS":
                    buffer = CsvToBytes(l, [(DataTypes.String, 0x30), (DataTypes.Int, 15), (DataTypes.Float, 15)], sect);
                    break;
                default:
                    Console.WriteLine($"Unknown section \"{sect}\", skipping");
                    continue;
            } 
endSwitch:
            _sstSection[sect].AddRange(buffer);
            _sstSizes[sect] = buffer.Length;
        }
        // write section binaries
        Console.WriteLine("Generating header");
        output.Write("FpnSst00"u8);
        output.Write(BitConverter.GetBytes(_sstSection.Count));
        var offset = 0x10 + 0x10 * _sstSection.Count;
        output.Write(BitConverter.GetBytes(offset));
        var preKey = "";
        foreach (var key in _sstSection.Keys)
        {
            if (output.Position % 0x10 != 0)
            {
                output.Position += 0x2;
                output.Write(BitConverter.GetBytes(GetDefaultSize(preKey)));
                output.Write(BitConverter.GetBytes(offset));
            }

            preKey = key; 
            Console.WriteLine($"Generating bytecode for {key}");
            var buffer = new byte[0x8];
            foreach (var (i, c) in key.Replace('_', '+').ToCharArray().Index())
            {
                if ((i == key.Length - 1) && (c == '_')) break;
                buffer[i] = Encoding.UTF8.GetBytes(c.ToString().Replace('+', '_'))[0];
            }

            output.Write(buffer);
            if (!_sstSection.TryGetValue(key, out var value)) value = [];
            var size = _sstSizes.GetValueOrDefault(key, GetDefaultSize(preKey));
            output.Write(BitConverter.GetBytes((short)value.Count));
            output.Write(BitConverter.GetBytes((short)size));
            output.Write(BitConverter.GetBytes(offset));
            var cPos = output.Position;
            output.Position = offset;
            foreach (var s in value)
                output.Write(s);
            output.Position = cPos;
            offset += value.Count * size;
            while (offset % 0x10 != 0)
                offset++;
        }

        while (output.Position % 0x10 != 0)
            output.WriteByte(0);
        return ("Operation completed successfully", input.Length);
    }

    private static short GetDefaultSize(string key)
    {
        return key switch
        {
            "SGKTBL" or "SGKIDX" or "MTTBLDEF" or "EVTIDX" => 0x4,
            "LNEWARP" or "LNEWARP_" or "KUOFS" or "REBIRTH" or "DRAWD" or "WALLD" or "REBIRTH_" or "REBIRTH+" => 0x8,
            "EVTINF" => 0xC,
            "SETBL" or "MTTBL" or "PTTBL" or "EVTBL" => 0x10,
            "SKYN" or "CAMN" or "LIGHTN" or "PATHN" or "BALLD" or "CAMKUD" or "BOSSN" or "STGNAME" or "KUIDX" => 0x20,
            "SEQN" or "STGINF" or "BOSS_REF" => 0x24,
            "CAMLD" or "EFFN" or "FNTN" => 0x28,
            "CAMD" or "PLIGHT" or "BOSS_TEX" => 0x30,
            "RECORD" => 0x34,
            "BALLN" or "INTN" or "EVENT"  => 0x40,
            "VABN" => 0x44,
            "KUINF" => 0x50,
            "IPUN" => 0x54,
            "CAMID" => 0x70,
            "BOSS_CAS" => 0xA8,
            "FLIESFPB" => 0xDA0,
            _ => 0x80
        };
    }

    private static byte GenerateCamDByte(string anchor, string xyz)
    {
        byte sum = 0;
        sum += (byte)(anchor == "Anchor" ? (byte)Math.Pow(2, 3) : 0);
        sum += (byte)(xyz.Contains('X') ? (byte)Math.Pow(2, 2) : 0);
        sum += (byte)(xyz.Contains('Y') ? (byte)Math.Pow(2, 1) : 0);
        sum += (byte)(xyz.Contains('Z') ? (byte)Math.Pow(2, 0) : 0);
        return sum;
    }
    
    private static byte[] CsvToBytes(string csv, List<(DataTypes, int)> structure, string sectionKey)
    {
        var ms = new MemoryStream();
        ms.SetLength(GetDefaultSize(sectionKey));
        var cSplit = csv.Split(',');
        var structureIdx = 0;
        var type = structure[0].Item1;
        var count = structure[0].Item2;
        foreach (var t in cSplit)
        {
            switch (type)
            {
                case DataTypes.Float:
                    float f = float.TryParse(t, out f) ? f : -1f;
                    ms.Write(BitConverter.GetBytes(f));
                    break;
                case DataTypes.Byte:
                    byte b = byte.TryParse(t, out b) ? b : (byte)255;
                    ms.WriteByte(b);
                    break;
                case DataTypes.Int:
                    int n = int.TryParse(t, out n) ? n : -1;
                    ms.Write(BitConverter.GetBytes(n));
                    break;
                case DataTypes.Short:
                    short s = short.TryParse(t, out s) ? s : (short)-1;
                    ms.Write(BitConverter.GetBytes(s));
                    break;
                case DataTypes.Long:
                    long l = long.TryParse(t, out l) ? l : -1;
                    ms.Write(BitConverter.GetBytes(l));
                    break;
                case DataTypes.String:
                    ms.Write(Encoding.UTF8.GetBytes(t));
                    if (t.Length < count)
                    {
                        ms.Position += count - t.Length;
                        count = 1;
                    }
                    break;
                case DataTypes.WString:
                    ms.Write(Encoding.Unicode.GetBytes(t));
                    if (t.Length < count)
                    {
                        ms.Position += count - t.Length * 2;
                        count = 1;
                    }
                    break;
                case DataTypes.SFmvType:
                    ms.WriteByte((byte)(t == "FmvType::LoRes" ? 3 : 2));
                    ms.Position += 3;
                    break;
                case DataTypes.SFadeInType:
                    ms.WriteByte((byte)(t == "FadeIn::White" ? 1 : 0));
                    ms.Position += 3;
                    break;
                case DataTypes.SFadeOutType:
                    ms.WriteByte((byte)(t == "FadeOut::White" ? 1 : 0));
                    ms.Position += 3;
                    break;
                case DataTypes.Dummy:
                    break;
                case DataTypes.Padding:
                default:
                    ms.Position++;
                    break;
            }
            count--;
            if (count != 0) continue;
            structureIdx++;
            if (structureIdx >= structure.Count) continue;
            type = structure[structureIdx].Item1;
            count = structure[structureIdx].Item2;
        }
        var ret = ms.ToArray();
        ms.Close();
        return ret;
    }
    
    private static byte[] PadString(string s, int size)
    {
        var data = Encoding.UTF8.GetBytes(s);
        while (data.Length < size)
        {
            data = [.. data, 0];
        }

        return data;
    }
}