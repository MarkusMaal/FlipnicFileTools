using System.Diagnostics;
using System.Globalization;
using System.Text;
using BigGustave;
using FlipnicLib.Vag;

namespace FlipnicLib;

public abstract class StaticUtils
{
    private static char[] Loaders = ['/', '-', '\\', '|'];
    public static int LoadIdx = 0;

    public static float LibVersion = 2.1f;
    public enum ControllerButtons : byte {
        Disabled = 0xFF,
        L2 = 0x0,
        R2,
        L1,
        R1,
        Triangle,
        Circle,
        Cross,
        Square, // no idea, but maybe 0x8 = Select?, 0xB = Start?, these won't work anyway, since they're reserved for stage status and pause menu
        L3 = 0x9,
        R3,
        DPadUp = 0xC,
        DPadRight,
        DPadDown,
        DPadLeft
    };
    
    public static bool LowMem { get; set; } = false;
    
    public static bool SimpleOutput { get; set; }
    public static bool Pal { get; set; }
    
    public static string LiveLoadStatus { get; set; }

    public static int WindowWidth { get; set; }

    public static string MsgFile { get; set; } = "";

    public static bool ExportEnvelopes { get; set; } = true;

    public static bool ExportVelocity { get; set; } = true;

    public static bool AltSf2Method { get; set; } = false;

    public static bool IsModeSet { get; set; } = false;
    
    public static void PrintLoader()
    {
        
        try
        {
            WindowWidth = Console.WindowWidth;
            Console.Write($"\r   {Loaders[LoadIdx++ / 1000]}");
            if (LoadIdx / 1000 >= Loaders.Length)
            {
                LoadIdx = 0;
            }
        }
        catch
        {
            LoadIdx = 0;
        }
    }
    
    public static float GetFloat(byte[] data, int offset)
    { 
        return BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
    }

    public static float GetHalf(byte[] data, int offset)
    {
        return (float)BitConverter.ToHalf(new[]{data[offset+1], data[offset]});
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

    public static long GetInt64(byte[] data, int offset)
    {
        return BitConverter.ToInt64(data.Skip(offset).Take(8).ToArray());
    }
    public static int GetInt32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
    }
    public static uint GetUInt32(byte[] data, int offset)
    {
        return BitConverter.ToUInt32(data.Skip(offset).Take(4).ToArray());
    }

    public static short GetInt16(byte[] data, int offset)
    {
        return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
    }
    public static ushort GetUInt16(byte[] data, int offset)
    {
        return BitConverter.ToUInt16(data.Skip(offset).Take(2).ToArray());
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

    public static string GenerateTable(string[] columns, List<string[]> rows, bool asCsv)
    {
        if ((rows.Count == 0) && !asCsv)
        {
            return "(none)";
        }
        var o = "";
        if (asCsv)
        {
            o = "***CSV\n" + string.Join(",", columns) + "\n";
            o = rows.Aggregate(o, (current, row) => current + (string.Join(",", row) + "\n"));
            return o;
        }
        var sep = "+";

        List<int> colSizes = [];
        for (var c = 0; c < columns.Length; c++)
        {
            var max = 0;
            if (rows.Count > 0)
            {
                max = rows.Select(row => row[c].Length).Max();
            }
            if (max < columns[c].Length) max = columns[c].Length;
            colSizes.Add(max);
        }

        var cI = -1;
        foreach (var cS in colSizes)
        {
            for (var j = 0; j < cS + 2; j++)
            {
                sep += "-";
            }
            sep += "+";
        }
        o += $"{sep}\n| ";
        cI = 0;
        foreach (var column in columns)
        {
            o = o + column.PadRight(colSizes[cI]) + " | ";
            cI++;
        }

        o += "\n";
        o += $"{sep}\n";
        if (LowMem)
        {
            Console.Write(o);
        }
        foreach (var row in rows)
        {
            if (LowMem)
            {
                o = "";
            }
            o += "| ";
            var line = "";

            cI = 0;
            foreach (var s in row)
            {
                line = line + s.PadRight(colSizes[cI]) + " | ";
                cI++;
            }
            o += line + "\n";
            if (LowMem)
            {
                Console.Write(o);
            }
        }

        o += $"{sep}\n";
        if (!LowMem) return o;
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
            _ => $"{bytes} B"
        };
    }

    public static void GenerateEmptyPng(string fileName, int width, int height)
    {
        var builder = PngBuilder.Create(width, height, true);
        var black = new Pixel(0, 0, 0, 0, false);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                builder.SetPixel(black, x, y);
            }
        }

        using var fs = new FileStream(fileName, FileMode.Create);
        builder.Save(fs);
        fs.Close();
    }

    public static void ProcessFFmpeg(string ffmpegPath, string ffmpegCommand)
    {
        var p = new Process
        {
            StartInfo =
            {
                FileName = ffmpegPath,
                Arguments = ffmpegCommand,
                UseShellExecute = true,
                CreateNoWindow = true,
            }
        };
        Console.WriteLine($"Running shell command: {ffmpegPath} {ffmpegCommand}");
        p.Start();
        p.WaitForExit();
    }
    
    public static void ConvertAudio(string outFile, string fileName, bool mono = false)
    {
        Console.Write("     Loading sound file to memory".PadRight(WindowWidth, ' '));
        PrintLoader();
        var data = File.ReadAllBytes(fileName);
        Console.Write("\r     Separating left and right channels".PadRight(WindowWidth, ' '));
        PrintLoader();
        List<byte> interleavedDataL = [];
        List<byte> interleavedDataR = [];
        for (var i = 0; i < data.Length; i += 0x400)
        {
            if (mono)
            {
                interleavedDataL.AddRange([.. data.Skip(i).Take(0x400)]);
                interleavedDataR.AddRange([.. data.Skip(i).Take(0x400)]);
                continue;
            }
            if (i % 0x800 == 0)
            {
                interleavedDataL.AddRange([.. data.Skip(i).Take(0x400)]);
            }
            else
            {
                interleavedDataR.AddRange([.. data.Skip(i).Take(0x400)]);
            }
        }
        
        Console.Write("\r     Converting to PCM".PadRight(WindowWidth, ' '));
        PrintLoader();
        using var msl = new MemoryStream(SonyVag.Decode([.. interleavedDataL]));
        using var msr = new MemoryStream(SonyVag.Decode([.. interleavedDataR]));
        using var ms = new MemoryStream();
        
        {
            Console.Write("\r     Generating WAV file".PadRight(WindowWidth, ' '));
            var bufL = new byte[2]; // 16-bit, 2 channels = 2+2 bytes
            var bufR = new byte[2];
            var i = 0;
            while (msl.Position < msl.Length)
            {
                try
                {
                    msl.ReadExactly(bufL, 0, bufL.Length);
                    msr.ReadExactly(bufR, 0, bufR.Length);
                    ms.Write(bufL);
                    ms.Write(bufR);
                    i++;
                    if (i % 0x100 == 0)
                    {
                        PrintLoader();
                    }
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }
            
            // Stereo, Signed 16-bit, 44100Hz
            Pcm.WriteWavHeader(ms, false, 2, 16, 44100, (int)ms.Length);
        
            Console.Write("\r     Saving WAV file".PadRight(WindowWidth, ' '));
            PrintLoader();
            // save WAV file
            var fs = new FileStream(outFile, FileMode.Create);
            ms.WriteTo(fs);
            fs.Close();
            Console.WriteLine($"\r   File saved as {outFile}".PadRight(WindowWidth, ' '));
        }
    }

    private static void HexStrToColor(string hex)
    {
        if (SimpleOutput) return;
        var bg = hex[0];
        var fg = hex[1]; 
        if ((bg == '-') && (fg == '-'))
        {
            Console.ResetColor();
            return;
        }

        if (bg != '-')
        {
            var bgI = Convert.FromHexString("0" + bg)[0];
            Console.BackgroundColor = (ConsoleColor)bgI;
        }

        if (fg == '-') return;
        var fgI = Convert.FromHexString("0" + fg)[0];
        Console.ForegroundColor = (ConsoleColor)fgI;
    }

    public static void DecodeColors(string encoded)
    {
        foreach (var _sect in encoded.Split('~'))
        {
            if (_sect.Length == 0) continue;
            var sect = _sect.Replace("::::", "~")[2..];
            var colorCode = _sect[..2].ToUpper();
            HexStrToColor(colorCode);
            Console.Write(sect);
        }
    }


    public static string SNote(Jam.Note noteStr)
    {
        return noteStr.ToString().Replace("Sharp", "#").Replace("Neg", "Ng");
    }
}