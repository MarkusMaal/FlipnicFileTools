using System.Globalization;
using System.Text;

namespace FlipnicFileTool;

public class StaticUtils
{
    private static char[] Loaders = ['/', '-', '\\', '|'];
    public static int LoadIdx = 0;

    public static void PrintLoader()
    {
        Console.Write($"\r   {Loaders[LoadIdx++/1000]}");
        if (LoadIdx / 1000 >= Loaders.Length) { LoadIdx = 0; }
    }
    
    public static float GetFloat(byte[] data, int offset)
    { 
        return BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
    }

    public static string GetString(byte[] data)
    {
        var chars = new List<char>();
        var offset = 0;
        while (data[offset] != 0x00)
        {
            chars.Add((char)data[offset]);
            offset++;
            if (offset >= data.Length) break;
        }
        return new string(chars.ToArray());
    }

    public static int GetInt32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
    }

    public static short GetInt16(byte[] data, int offset)
    {
        return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
    }

    public static string GetStringAt(byte[] data, int offset)
    {
        var chars = new List<char>();
        while (data[offset] != 0x00)
        {
            chars.Add((char)data[offset]);
            offset++;
        }
        return new string(chars.ToArray());
    }

    public static string GetFixedUtf16String(byte[] data, int offset, int length)
    {
        return Encoding.Unicode.GetString(data.Skip(offset).Take(length).ToArray());
    }

    public static string DotFloatString(float f)
    {
        return f.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }

    public static string GenerateTable(string[] columns, List<string[]> rows, int colSize = 15)
    {
        var width = (colSize+3) * columns.Length; 
        var sep = "+";
        var o = "";
        
        for (var i = 1; i <= width; i++)
        {
            if (i % (colSize+3) == 0) sep += "+";
            else sep += "-";
        }
        o += $"{sep}\n| ";
        o = columns.Aggregate(o, (current, t) => current + t.PadRight(colSize) + " | ");

        o += "\n";
        o += $"{sep}\n";
        if (Program.LowMem)
        {
            Console.Write(o);
        }
        foreach (var row in rows)
        {
            if (Program.LowMem)
            {
                o = "";
            }
            o += "| ";
            var line = row.Aggregate("", (current, s) => current + s.PadRight(colSize) + " | ");
            o += line + "\n";
            if (Program.LowMem)
            {
                Console.Write(o);
            }
        }

        o += $"{sep}\n";
        if (!Program.LowMem) return o;
        Console.Write(o);
        o = "";
        return o;
    }

    public static string GetFilesizeString(long bytes)
    {
        return bytes switch
        {
            > 1073741824 => $"{DotFloatString((float)Math.Round(bytes / 1073741824f, 2))} GiB",
            > 1048576 => $"{DotFloatString((float)Math.Round(bytes / 1048576f, 2))} MiB",
            > 1024 => $"{DotFloatString((float)Math.Round(bytes / 1024f, 2))} kiB",
            _ => $"{bytes} b"
        };
    }
}