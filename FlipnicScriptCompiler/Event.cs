using System.Globalization;
using System.Text;

namespace FlipnicScriptCompiler;

public readonly struct Event(byte[] data)
{
    public int Magic { get; } = BitConverter.ToInt32([.. data.Take(4)], 0);
    public string Label { get; } = Encoding.UTF8.GetString([..data.Skip(4).Take(0x20)]).TrimEnd('\0'); 
    public int Arg1 { get; } = BitConverter.ToInt32([.. data.Skip(0x24).Take(4)]);
    public int Arg2 { get; } = BitConverter.ToInt32([.. data.Skip(0x28).Take(4)]);
    public int Arg3 { get; } = BitConverter.ToInt32([.. data.Skip(0x2C).Take(4)]);
    public int Arg4 { get; } = BitConverter.ToInt32([.. data.Skip(0x30).Take(4)]);
    public int Arg5 { get; } = BitConverter.ToInt32([.. data.Skip(0x34).Take(4)]);
    public int Arg6 { get; } = BitConverter.ToInt32([.. data.Skip(0x38).Take(4)]);
    public int Arg7 { get; } = BitConverter.ToInt32([.. data.Skip(0x3C).Take(4)]);

    public static byte[] Parse(string line)
    {
        var cleanLine = line.TrimStart('\t').TrimStart(' ').TrimEnd(' ');
        Console.WriteLine("\tCompiling " + cleanLine);
        var ms = new MemoryStream();
        ms.SetLength(0x40);
        if (cleanLine == "}")
        {
            ms.WriteByte(8);
        }
        else if (cleanLine.StartsWith("stop"))
        {
            ms.WriteByte(1);
        }
        else if (cleanLine.StartsWith("func"))
        {
            var label = cleanLine.Split(' ')[1];
            if (label.Contains('('))
                label = label.Split('(')[0];
            ms.Position = 0x04;
            ms.Write(Encoding.UTF8.GetBytes(label));
        } else if (cleanLine.StartsWith("goto"))
        {
            var label = "";
            if (cleanLine.Contains(' '))
                label = cleanLine.Split(' ')[1];
            if (label.Contains('('))
                label = label.Split('(')[0];
            ms.WriteByte(2);
            ms.Position = 0x04;
            ms.Write(Encoding.UTF8.GetBytes(label));
        } else if (cleanLine.StartsWith("endif"))
        {
            ms.WriteByte(5);
        } else if (cleanLine.StartsWith("if2"))
        {
            ms.WriteByte(3);
            var ifSides = cleanLine[3..].TrimStart(' ')[1..][..^1].TrimEnd(' ').TrimStart(' ').Replace(" == ", "==").Split("==");
            var label = ifSides[0].Split('[')[0];
            if (label.Contains('('))
                label = label.Split('(')[0];
            var arg3 = int.Parse(ifSides[0].Split('[')[1].Split(']')[0]);
            var arg2 = int.Parse(ifSides[1].Replace("0x", ""), NumberStyles.HexNumber);
            ms.Position = 4;
            ms.Write(Encoding.UTF8.GetBytes(label));
            ms.Position = 0x24;
            ms.WriteByte(2);
            ms.Position = 0x28;
            ms.Write(BitConverter.GetBytes(arg2));
            ms.Write(BitConverter.GetBytes(arg3));
        } else if (cleanLine.StartsWith("if"))
        {
            ms.WriteByte(3);
            var ifSides = cleanLine[2..].TrimStart(' ')[1..][..^1].TrimEnd(' ').TrimStart(' ').Replace(" == ", "==").Split("==");
            var label = ifSides[0].Split('[')[0];
            if (label.Contains('('))
                label = label.Split('(')[0];
            var arg3 = int.Parse(ifSides[0].Split('[')[1].Split(']')[0]);
            var arg2 = int.Parse(ifSides[1].Replace("0x", ""), NumberStyles.HexNumber);
            ms.Position = 4;
            ms.Write(Encoding.UTF8.GetBytes(label));
            ms.Position = 0x24;
            ms.WriteByte(1);
            ms.Position = 0x28;
            ms.Write(BitConverter.GetBytes(arg2));
            ms.Write(BitConverter.GetBytes(arg3));
        } else if (cleanLine.StartsWith(':'))
        {
            ms.WriteByte(7);
            ms.Position = 4;
            ms.Write(Encoding.UTF8.GetBytes(cleanLine[1..]));
        } else if (cleanLine.StartsWith("play_video"))
        {
            var funcArgs = cleanLine[11..][..^1].Replace(", ", ",").Split(',');
            ms.WriteByte(0xE);
            ms.Position = 0x28;
            ms.Write(BitConverter.GetBytes(int.Parse(funcArgs[0])));
            ms.Write(BitConverter.GetBytes(funcArgs[1] == "RANDOMIZER::ENABLED"));
            ms.Position = 0x30;
            ms.Write(BitConverter.GetBytes(int.Parse(funcArgs[2])));
        } else if (cleanLine.StartsWith("trigger_"))
        {
            var label = cleanLine[8..].Split('(')[0];
            var args = cleanLine.Split('(')[1].Split(')')[0].Replace(", ", ",").Split(',');
            ms.WriteByte(9);
            ms.Position = 0x4;
            ms.Write(Encoding.UTF8.GetBytes(label));
            ms.Position = 0x24;
            ms.WriteByte(1);
            ms.Position = 0x28;
            foreach (var arg in args)
            {
                if (arg == "") continue;
                ms.Write(BitConverter.GetBytes(int.Parse(arg)));
            }
        } else if (cleanLine.StartsWith("op_"))
        {
            var label = "";
            if (cleanLine.ToCharArray().Count(c => c == '_') > 1)
            {
                label = string.Join('_', cleanLine.Split('_').Skip(2));
                if (label.Contains('('))
                    label = label.Split('(')[0];
            }

            var op = -1;
            
            op = cleanLine.ToCharArray().Count(c => c == '_') > 1
                ? int.Parse(cleanLine.Split('_')[1][2..], NumberStyles.HexNumber)
                : int.Parse(cleanLine.Split('_')[1][2..].Split('(')[0], NumberStyles.HexNumber);
            var args = cleanLine.Split('(')[1].Split(')')[0].Replace(", ", ",").Split(',');
            ms.Write(BitConverter.GetBytes(op));
            ms.Write(Encoding.UTF8.GetBytes(label));
            ms.Position = 0x24;
            foreach (var arg in args)
            {
                if (arg == "") continue;
                ms.Write(BitConverter.GetBytes(int.Parse(arg)));
            }
        }
        else
        {
            var label = cleanLine.Split('[')[0];
            if (label.Contains('('))
                label = label.Split('(')[0];
            var idx_pre = cleanLine.Split('[')[1];
            var idx = idx_pre.Split(']')[0];
            var value = cleanLine.Replace(" = ", "=").Split("=")[1];
            ms.WriteByte(9);
            ms.Position = 4;
            ms.Write(Encoding.UTF8.GetBytes(label));
            ms.Position = 0x24;
            ms.WriteByte(3);
            ms.Position = 0x28;
            ms.Write(BitConverter.GetBytes(int.Parse(value)));
            ms.Write(BitConverter.GetBytes(int.Parse(idx)));
        }
        var ret = ms.ToArray();
        ms.Close();
        return [.. ret.Take(0x40)];
    }
}