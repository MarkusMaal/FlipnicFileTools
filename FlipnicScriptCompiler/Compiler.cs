using System.Text;

namespace FlipnicScriptCompiler;

public class Compiler
{

    private Dictionary<string, List<byte[]>> sstSection = new(); 
    private Dictionary<string, int> sstSizes = new();
    
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
                sstSection[sect] = [];
                Console.WriteLine($"Parsing {sect}");
                continue;
            }

            var buffer = new byte[0x20];
            byte[] idx;
            switch (sect)
            {
                case "BALLN":
                case "SKYN":
                case "CAMN":
                case "LIGHTN":
                case "PATHN":
                case "KUIDX":
                case "BOSSN":
                case "STGNAME":
                    foreach (var (j, b) in Encoding.UTF8.GetBytes(l).Take(0x20).Index())
                        buffer[j] = b;
                    break;
                case "VABN":
                case "INTN":
                    var lSplit = l.Split(',');
                    buffer = new byte[lSplit.Length > 2 ? 0x44 : 0x40];
                    foreach (var (j, b) in Encoding.UTF8.GetBytes(lSplit[0]).Index())
                        buffer[j] = b;
                    foreach (var (j, b) in Encoding.UTF8.GetBytes(lSplit[1]).Index())
                        buffer[0x20 + j] = b;
                    if (buffer.Length > 0x40)
                    {
                        idx = BitConverter.GetBytes(int.Parse(lSplit[2]));
                        for (var j = 0; j < 4; j++)
                            buffer[0x40 + j] = idx[j];
                    }
                    break;
                case "SEQN":
                case "EFFN":
                case "FNTN":
                case "STGINF":
                    lSplit = l.Split(',');
                    buffer = new byte[lSplit[1].EndsWith('L') ? 0x28 : 0x24];
                    foreach (var (j, b) in Encoding.UTF8.GetBytes(lSplit[0]).Index())
                        buffer[j] = b;
                    idx = lSplit[1].EndsWith('L') ? BitConverter.GetBytes(long.Parse(lSplit[1][..^1])) : BitConverter.GetBytes(int.Parse(lSplit[1]));
                    for (var j = 0; j < buffer.Length - 0x20; j++)
                        buffer[0x20 + j] = idx[j];
                    break;
                case "BALLD":
                    var fl = BitConverter.GetBytes(float.Parse(l));
                    for (var j = 0; j < 4; j++)
                        buffer[j] = fl[j];
                    break;
                case "LNEWARP":
                case "KUOFS":
                    lSplit = l.Split(',');
                    buffer = new byte[8];
                    foreach (var (j, b) in lSplit.Index())
                    {
                        var s = BitConverter.GetBytes(short.Parse(b));
                        buffer[j * 2] = s[0];
                        buffer[j * 2 + 1] = s[1];
                    }
                    break;
                case "EVTINF":
                    lSplit = l.Split(',');
                    buffer = new byte[0xC];
                    var stgIdx = BitConverter.GetBytes(int.Parse(lSplit[0]));
                    var msgIdx = BitConverter.GetBytes(int.Parse(lSplit[4]));
                    for (var j = 0; j < 4; j++)
                        buffer[j] = stgIdx[j];
                    for (var j = 8; j < 12; j++)
                        buffer[j] = msgIdx[j - 8];
                    buffer[4] = byte.Parse(lSplit[1]);
                    buffer[6] = byte.Parse(lSplit[2]);
                    buffer[7] = byte.Parse(lSplit[3]);
                    break;
                case "RECORD":
                    lSplit = l.Split(',');
                    List<byte> lB =
                    [
                        .. BitConverter.GetBytes(long.Parse(lSplit[0])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[1])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[2])),
                        .. PadString(lSplit[3], 0x20),
                        .. BitConverter.GetBytes(int.Parse(lSplit[4])),
                    ];
                    buffer = [.. lB];
                    break;
                case "CAMD":
                    lSplit = l.Split(',');
                    var stiffy = lSplit[8].Split('/');
                    lB =
                    [
                        .. BitConverter.GetBytes(int.Parse(lSplit[0])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[1])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[2])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[3])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[4])),
                        .. BitConverter.GetBytes(0),
                        .. BitConverter.GetBytes((int)GenerateCamDByte(lSplit[5], lSplit[6])),
                        .. BitConverter.GetBytes(sstSection["CAMN"].FindIndex(p => lSplit[7].TrimEnd('\0') == Decompiler.GetString(p))),
                        .. BitConverter.GetBytes(0),
                        .. BitConverter.GetBytes(float.Parse(stiffy[0])),
                        .. BitConverter.GetBytes(float.Parse(stiffy[1])),
                        .. BitConverter.GetBytes(float.Parse(stiffy[2]))
                    ];
                    buffer = [.. lB];
                    break;
                case "CAMLD":
                    lSplit = l.Split(',');
                    lB =
                    [
                        .. BitConverter.GetBytes(int.Parse(lSplit[0])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[1])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[2])),
                        .. BitConverter.GetBytes(float.Parse(lSplit[3])),
                        .. BitConverter.GetBytes(float.Parse(lSplit[4])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[5])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[6])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[7])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[8])),
                        .. BitConverter.GetBytes(int.Parse(lSplit[9])),
                    ];
                    buffer = [.. lB];
                    break;
                case "IPUN":
                    lSplit = l.Split(',');
                    lB =
                    [
                        .. PadString(lSplit[0], 0x20),
                        .. PadString(lSplit[1], 0x20),
                        .. BitConverter.GetBytes(lSplit[2] == "FmvType::LoRes" ? 2 : 3),
                        .. BitConverter.GetBytes(int.Parse(lSplit[3])),
                        .. BitConverter.GetBytes(lSplit[4] == "FadeIn::White" ? 1 : 0),
                        .. BitConverter.GetBytes(int.Parse(lSplit[5])),
                        .. BitConverter.GetBytes(lSplit[6] == "FadeOut::White" ? 1 : 0),
                    ];
                    buffer = [.. lB];
                    break;
                default:
                    return ( $"Unknown section name \"{sect}\"", i);
            }
            sstSection[sect].AddRange(buffer);
            sstSizes[sect] = buffer.Length;
        }
        // write section binaries
        Console.WriteLine("Generating header");
        output.Write("FpnSst00"u8);
        output.Write(BitConverter.GetBytes(sstSection.Count));
        var offset = 0x10 + 0x10 * sstSection.Count;
        output.Write(BitConverter.GetBytes(offset));
        foreach (var key in sstSection.Keys)
        {
            Console.WriteLine($"Generating bytecode for {key}");
            var buffer = new byte[0x8];
            foreach (var (i, c) in key.ToCharArray().Index())
            {
                buffer[i] = Encoding.UTF8.GetBytes(c.ToString())[0];
            }

            output.Write(buffer);
            output.Write(BitConverter.GetBytes((short)sstSection[key].Count));
            output.Write(BitConverter.GetBytes((short)sstSizes[key]));
            output.Write(BitConverter.GetBytes(offset));
            var cPos = output.Position;
            output.Position = offset;
            foreach (var s in sstSection[key])
                output.Write(s);
            output.Position = cPos;
            offset += sstSection[key].Count * sstSizes[key];
            while (offset % 0x10 != 0)
                offset++;
        }

        while (output.Position % 0x10 != 0)
            output.WriteByte(0);
        return ("Operation completed successfully", input.Length);
    }

    private byte GenerateCamDByte(string anchor, string xyz)
    {
        byte sum = 0;
        sum += (byte)(anchor == "Anchor" ? (byte)Math.Pow(2, 0) : 0);
        sum += (byte)(xyz.Contains('X') ? (byte)Math.Pow(2, 1) : 0);
        sum += (byte)(xyz.Contains('Y') ? (byte)Math.Pow(2, 2) : 0);
        sum += (byte)(xyz.Contains('Z') ? (byte)Math.Pow(2, 3) : 0);
        return sum;
    }
    
    private byte[] PadString(string s, int size)
    {
        var data = Encoding.UTF8.GetBytes(s);
        while (data.Length < size)
        {
            data = [.. data, 0];
        }

        return data;
    }
}