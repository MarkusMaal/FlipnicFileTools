using System.Diagnostics;
using System.Globalization;
using BigGustave;
using FlipnicLib.Formats;
using FlipnicLib.Formats.Jam;
using SonyVag = FlipnicLib.Formats.Vag.SonyVag;

namespace FlipnicLib;

public class StaticUtils
{
    public static string DisclaimerText =>
        "This software is provided to you free of charge AS IS without a warranty. If you paid for this software, you should ask for a refund. The copyrights of original Flipnic game assets belong to Japan Studio of Sony Interactive Entertainment (a.k.a. SCEI) and these assets are not distributed with this software.";
    
    private static readonly char[] Loaders = ['/', '-', '\\', '|'];
    private static int LoadIdx;

    public static int[] AdsrMultipliers = [1200, 1200, 1400, 1200, 320];

    private static DateTime LastUpdate { get; set; } = DateTime.Now;

    public static readonly float LibVersion = 2.4f;
    public static readonly bool IsBeta = true;
    
    public static bool LowMem { get; set; }
    
    public static bool SimpleOutput { get; set; }
    public static bool Pal { get; set; }

    /// <summary>
    /// Allows you to specify a method to run when the value of "LiveLoadStatus" is changed, for example:<br/>
    /// <br/>
    /// StaticUtils.UpdateText += (v) => Console.WriteLine($"New value: {v}");
    /// </summary>
    public static event UpdateText? TextUpdate;
    public delegate void UpdateText(string? text);
    public static string? LiveLoadStatus
    {
        get;
        set
        {
            field = value;
            TextUpdate?.Invoke(value);
        }
    }

    public static int WindowWidth { get; set; }

    public static string MsgFile { get; set; } = "";

    public static bool ExportEnvelopes { get; set; } = true;

    public static bool AltSf2Method { get; set; } = false;

    public static bool IsModeSet { get; set; }

    public static short ReverbStrength { get; set; } = 70;

    public static bool AlternateNormals { get; set; }

    public static bool ForceNoColors { get; set; } = false;
    
    public static bool ForceBruteForce { get; set; }
    
    /// <summary>
    /// Display an animated spinning line loader
    /// </summary>
    public static void PrintLoader()
    {
        try
        {
            WindowWidth = Console.WindowWidth;
            if (DateTime.Now - LastUpdate >= TimeSpan.FromMilliseconds(100))
            {
                LoadIdx++;
                if (LoadIdx >= Loaders.Length) LoadIdx = 0;
                LastUpdate = DateTime.Now;
            }
            Console.Write($"\r   {Loaders[LoadIdx]}");
        } catch (IOException)
        {
            // no command window, just avoid printing the loader
        }
    }
    
    
    private static string PadBoth(string str, int length)
    {
        var spaces = length - str.Length;
        var padLeft = spaces / 2 + str.Length;
        return str.PadLeft(padLeft).PadRight(length);
    }
    
    /// <summary>
    /// Generates an ASCII table with the data provided
    /// </summary>
    /// <param name="columns">Table headers (the first row)</param>
    /// <param name="rows">Every subsequent row of the table (the length of each string[] inside the list should be equal to the length of columns)</param>
    /// <param name="asCsv">Return a CSV table instead of ASCII table</param>
    /// <returns>A table containing the data provided as string</returns>
    public static string GenerateTable(string[] columns, List<string[]> rows, bool asCsv)
    {
        if ((rows.Count == 0) && !asCsv)
        {
            return "(none)";
        }
        var o = "";
        if (asCsv)
        {
            o = "***TABLE***\n" + string.Join(",", columns) + "\n";
            o = rows.Aggregate(o, (current, row) => current + (string.Join(",", row) + "\n"));
            o += "***END***";
            return o;
        }
        var sep = "+";

        List<int> colSizes = [];
        for (var c = 0; c < columns.Length; c++)
        {
            var max = rows.Max(row => row[c].Length);
            if (max < columns[c].Length) max = columns[c].Length;
            colSizes.Add(max);
        }

        foreach (var cS in colSizes)
        {
            for (var j = 0; j < cS + 2; j++)
            {
                sep += "-";
            }
            sep += "+";
        }
        o += $"{sep}\n| ";
        var cI = 0;
        foreach (var column in columns)
        {
            o = o + PadBoth(column, colSizes[cI]) + " | ";
            cI++;
        }
        o = o[..^1];

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
            o += line[..^1] + "\n";
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

    /// <summary>
    /// Converts a numeric size value to a human-friendly string to describe the file size in B, kiB, MiB or GiB
    /// </summary>
    /// <param name="bytes">Filesize in bytes</param>
    /// <returns>Formatted filesize string (e.g. 5.21MiB)</returns>
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

    /// <summary>
    /// Creates a fully transparent blank PNG file with the specified width and height.
    /// </summary>
    /// <param name="fileName">Full path of the output file</param>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
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

    /// <summary>
    /// Generate a PNG file with the magenta/black checkerboard pattern
    /// </summary>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
    /// <param name="black">First color used by the checkerboard (default: black)</param>
    /// <param name="magenta">Second color used by the checkerboard (default: magenta)</param>
    public static Stream GenerateCheckerboardPng(int width, int height, Pixel? black = null, Pixel? magenta = null)
    {
        var output = new MemoryStream();
        var builder = PngBuilder.Create(width, height, true);
        black ??= new Pixel(0, 0, 0, 255, false);
        magenta ??= new Pixel(255, 0, 255, 255, false);
        var invert = false;
        for (var y = 0; y < height; y++)
        {
            if (y % 16 == 0) invert = !invert;
            var invert2 = invert;
            for (var x = 0; x < width; x++)
            {
                if (x % 16 == 0)  invert2 = !invert2;
                builder.SetPixel(invert2 ? (Pixel)black : (Pixel)magenta, x, y);
            }
        }

        builder.Save(output);
        output.Position = 0;
        return output;
    }

    /// <summary>
    /// Runs FFmpeg command with the OS shell
    /// </summary>
    /// <param name="ffmpegPath">Full path to FFmpeg executable</param>
    /// <param name="ffmpegCommand">Arguments to pass to FFmpeg (separated by spaces)</param>
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

    /// <summary>
    /// Convert Sony Compressed ADPCM audio to PCM
    /// </summary>
    /// <param name="outFile">Full path to output .WAV file</param>
    /// <param name="data">Binary data containing the compressed ADPCM audio with 0x400 interleave</param>
    /// <param name="mono">Does the audio file only have 1 channel? If yes, this method won't interleave the data.</param>
    /// <param name="sampleRate">Sample rate of the audio in Hz (see https://en.wikipedia.org/wiki/Sampling_(signal_processing)#Audio_sampling)</param>
    public static void ConvertAudio(string outFile, byte[] data, bool mono = false, int sampleRate = 44100)
    {
        File.WriteAllBytes(Path.GetTempPath() + "/temp.vag", data);
        ConvertAudio(outFile, Path.GetTempPath() + "/temp.vag", mono, sampleRate);
        File.Delete(Path.GetTempPath() + "/temp.vag");
    }
    
    /// <summary>
    /// Convert Sony Compressed ADPCM audio to PCM
    /// </summary>
    /// <param name="outFile">Full path to output .WAV file</param>
    /// <param name="fileName">Full path to input file containing compressed ADPCM audio with 0x400 interleave</param>
    /// <param name="mono">Does the audio file only have 1 channel? If yes, this method won't interleave the data.</param>
    /// <param name="sampleRate">Sample rate of the audio in Hz (see https://en.wikipedia.org/wiki/Sampling_(signal_processing)#Audio_sampling)</param>
    public static void ConvertAudio(string outFile, string fileName, bool mono = false, int sampleRate = 44100)
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
                    if (i == msl.Length/2 - 1342)
                    {
                        break;
                    }
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
            var riff = new Riff(sampleRate)
            {
                NumChannels = 2,
                BitsPerSample = 16,
                data = ms.ToArray(),
            };

            ms.Position = 0;
            ms.Write(riff.GetBytes());
            Console.Write("\r     Saving WAV file".PadRight(WindowWidth, ' '));
            PrintLoader();
            // save WAV file
            var fs = new FileStream(outFile, FileMode.Create);
            ms.WriteTo(fs);
            fs.Close();
            Console.WriteLine($"\r   File saved as {outFile}".PadRight(WindowWidth, ' '));
        }
    }

    private static void HexStrToColor(string hex) // internal method, do not touch
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

    /// <summary>
    /// Paints text characters inside the string to specific colors specified by my custom encoding and displays them.<br/><br/>
    ///
    /// Colors are encoded like so: ~[BG][FG], where BG represents a background color using a hex nibble (0-F)<br/>
    /// or dash character (-), FG is the same, but for foreground color. The first character of the encoded string MUST<br/>
    /// be a tilde (~).<br/><br/>
    ///
    /// Example: "~1FThis is white on blue.~-- This is default. ~-AThis is green on default.~4-This is default on red.~--"
    /// </summary>
    /// <param name="encoded">The encoded text</param>
    public static void DecodeColors(string encoded)
    {
        foreach (var sect2 in encoded.Split('~'))
        {
            if (sect2.Length == 0) continue;
            var sect = sect2.Replace("::::", "~")[2..];
            var colorCode = sect2[..2].ToUpper();
            HexStrToColor(colorCode);
            Console.Write(sect);
        }
    }


    /// <summary>
    /// Shortens the Note value
    /// </summary>
    /// <param name="noteStr">Unshortened string</param>
    /// <returns>Shortened value, e.g. Sharp is replaced with #</returns>
    public static string SNote(Note noteStr)
    {
        return noteStr.ToString().Replace("Sharp", "#").Replace("Neg", "Ng");
    }

    /// <summary>
    /// Export raw model data and texture to Wavefront OBJ, MTL and PNG
    /// </summary>
    /// <param name="fileName">Full path to output OBJ file (including extension)</param>
    /// <param name="vertices">Array containing raw model data (each chunk is 7*sizeof(float), where first 2 items are XY UV coordinates, next 3 items are XYZ vertex coordinates and final 3 items are XYZ normal coordinates)</param>
    /// <param name="texture">Texture object (either Tim or Tim2 is accepted here)</param>
    /// <param name="ignoreNormals">Optional: If set to true, ensures that no normal vectors will get exported to the final OBJ file</param>
    public static void ExportObj(string fileName, float[] vertices, object? texture, bool ignoreNormals = false)
    {
        // generate .png file
        var hasTexture = true;
        switch (texture)
        {
            case Tim2 tm2:
                tm2.SavePng(new FileStream(fileName[..^4] + ".png", FileMode.Create, FileAccess.Write));
                break;
            case Tim tm:
                tm.SavePng(new FileStream(fileName[..^4] + ".png", FileMode.Create, FileAccess.Write));
                break;
            case byte[] ba:
                File.WriteAllBytes(fileName[..^4] + ".png", ba);
                break;
            default:
                hasTexture = false;
                break;
        }
            
        // generate .mtl file
        if (hasTexture)
        {
            using var mtlwriter = new StreamWriter(fileName[..^4] + ".mtl");
            mtlwriter.WriteLine($"newmtl {new FileInfo(fileName).Name[..^4]}");
            mtlwriter.WriteLine($"map_Kd {new FileInfo(fileName).Name[..^4]}.png");
            mtlwriter.Close();
            Console.WriteLine($"Saved as: {fileName[..^4]}.mtl");
        }

        var vertexCount = vertices.Length / 8;
        using var writer = new StreamWriter(fileName);
        var culture = CultureInfo.InvariantCulture;
        if (hasTexture)
        {
            writer.WriteLine($"mtllib {new FileInfo(fileName).Name[..^4]}.mtl");
            writer.WriteLine($"usemtl {new FileInfo(fileName).Name[..^4]}");
        }

        // Write vertex positions and texture coordinates
        for (var i = 0; i < vertexCount; i++)
        {
            var u = vertices[i * 8 + 0];
            var v = vertices[i * 8 + 1];
            var x = vertices[i * 8 + 2];
            var y = vertices[i * 8 + 3];
            var z = vertices[i * 8 + 4];
            var nx = vertices[i * 8 + 5];
            var ny = vertices[i * 8 + 6];
            var nz = vertices[i * 8 + 7];

            writer.WriteLine($"v {x.ToString(culture)} {y.ToString(culture)} {z.ToString(culture)}");
            writer.WriteLine($"vt {u.ToString(culture)} {v.ToString(culture)}");
            if (!ignoreNormals) writer.WriteLine($"vn {nx.ToString(culture)} {ny.ToString(culture)} {nz.ToString(culture)}");
        }

        // Write face assuming every 3 vertices = 1 triangle
        for (var i = 0; i < vertexCount; i += 3)
        {
            var v1 = i + 1;
            var v2 = i + 2;
            var v3 = i + 3;

            writer.WriteLine($"f {v1}/{v1} {v2}/{v2} {v3}/{v3}");
        }

        writer.Close();
        Console.WriteLine($"Saved as: {fileName}");
    }

    /// <summary>
    /// Converts a float to string with the en-US culture
    /// </summary>
    /// <param name="f">The float you want to stringify</param>
    /// <returns>Float formatted as string where the decimal point is a period (.)</returns>
    public static string DotFloatString(float f)
    {
        return f.ToString(CultureInfo.CreateSpecificCulture("en-US"));
    }
}