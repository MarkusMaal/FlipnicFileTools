using FlipnicLib;

namespace FlipnicFileTool.Tools;

public class VideoTools
{
    
    private string FileName { get; set; }
    private string Output { get; set; }
    private string FFmpegPath { get; set; }
    
    private static bool CropAlpha { get; set; }
    private static bool CropRgb { get; set; }
    
    private static int ScaleFactor { get; set; } = 1;
    
    private static string IntFile { get; set; } = "";
    


    public VideoTools(Config cfg)
    {
        FileName = cfg.FileName;
        Output = cfg.Output;
        FFmpegPath = cfg.FFmpegPath;
        CropAlpha = cfg.CropAlpha;
        CropRgb = cfg.CropRgb;
        ScaleFactor = cfg.ScaleFactor;
        IntFile = cfg.IntFile ?? "";

        switch (cfg.Mode)
        {
            
            case Enums.Modes.ConvertIpu:
                Ipu.IpuConvert(FileName, Output, FFmpegPath);
                break;
            case Enums.Modes.ListPssStreams:
                Console.WriteLine(new Pss(FileName).ListPss(File.OpenRead(FileName)));
                break;
            case Enums.Modes.ExtractPssStreams:
                new Pss(FileName).ListPss(File.OpenRead(FileName), true, Output);
                break;
            case Enums.Modes.ShowIpu:
                Console.WriteLine(Ipu.GetInfoAsString(File.OpenRead(FileName)));
                break;
            case Enums.Modes.GeneratePss:
                Console.WriteLine("Using audio stream: " + cfg.IntFile);
                Console.WriteLine("Video standard: " + (StaticUtils.Pal ? "PAL" : "NTSC") + " (" + (cfg.Progressive ? "Progressive" : "Interlaced") + ")");
                Pss.MergeStreams(new FileStream(FileName, FileMode.Open, FileAccess.Read), new FileStream(IntFile,  FileMode.Open, FileAccess.Read), new FileStream(Output, FileMode.Create, FileAccess.Write), cfg.Progressive, StaticUtils.Pal);
                break;
            case Enums.Modes.IpuFix:
                FixIpu(new FileStream(cfg.FileName, FileMode.Open, FileAccess.ReadWrite), StaticUtils.Pal, cfg.Progressive);
                break;
            case Enums.Modes.ConvertPssMpeg:
                ConvertPssMpeg();
                break;
        }
    }

    private static void FixIpu(Stream ipuFile, bool pal, bool progressive)
    {
        Console.WriteLine("Adding IPU magic");
        ipuFile.Position = 0;
        ipuFile.Write("ipum"u8);
        Console.WriteLine("Counting frames...");
        ipuFile.Position = 0x10;
        var frames = 0;
        var shiftRegister = "\0\0\0\0"u8.ToArray();
        while (ipuFile.Position < ipuFile.Length)
        {
            shiftRegister[0] = shiftRegister[1];
            shiftRegister[1] = shiftRegister[2];
            shiftRegister[2] = shiftRegister[3];
            shiftRegister[3] = (byte)ipuFile.ReadByte();
            if (shiftRegister[0] + shiftRegister[1] == 0 && shiftRegister[2] == 1 && shiftRegister[3] == 0xB0)
            {
                Console.Write($"\rFrames found: {frames}\t({StaticUtils.DotFloatString((float)Math.Round((ipuFile.Position / (double)ipuFile.Length * 100f), 0)).PadLeft(2, '0')}% complete)");
                frames++;
            }
        }
        Console.WriteLine();
        int end;
        if (!(shiftRegister[0] + shiftRegister[1] == 0 && shiftRegister[2] == 1 && shiftRegister[3] == 0xB1))
        {
            Console.WriteLine("Adding missing delimiter and end code");
            frames++;
            end = (int)ipuFile.Position;
            ipuFile.Write([0, 0, 1, 0xB0]);
            ipuFile.Write([0, 0, 1, 0xB1]);
        }
        else
        {
            end = (int)ipuFile.Position - 0x8;
        }
        Console.WriteLine("Write remaining bytes to the header");
        Console.WriteLine($"\tEnd offset: 0x{end:X}");
        ipuFile.Position = 4;
        ipuFile.Write(BitConverter.GetBytes(end));
        short height = 512;
        short width = 256;
        if (!progressive)
        {
            width = 512;
            if (!pal) height = 448;
        }
        Console.WriteLine($"\tResolution: {width}x{height}");
        ipuFile.Write(BitConverter.GetBytes(width));
        ipuFile.Write(BitConverter.GetBytes(height));
        Console.WriteLine("\tFrame count: " + frames);
        ipuFile.Write(BitConverter.GetBytes(frames));
        ipuFile.Close();
        Console.WriteLine("Done!");
    }

    /// <summary>
    /// Converts .PSS file directly to x264
    /// </summary>
    private void ConvertPssMpeg()
    {
        new Pss(FileName).ListPss(File.OpenRead(FileName), true, new FileInfo(Output).Directory!.FullName);
        var nf = Path.Combine(new FileInfo(Output).Directory!.FullName, new FileInfo(FileName).Name);
        Ipu.IpuConvert(nf + ".IPU", nf + ".TEMP.M2V", FFmpegPath);
        var exist = true;
        var streams = 0;
        while (exist)
        {
            if (File.Exists(
                    nf +
                    $".{++streams}.INT"))
            {
                FileName =
                    nf +
                    $".{streams}.INT";
                StaticUtils.ConvertAudio(nf + $".{streams}.WAV", FileName);
                continue;
            }

            exist = false;
        }

        var ffmpegCommand = $"-y -i \"{nf}.TEMP.M2V\" -i ";
        List<string> audioFiles = [];
        for (var i = 1; i < streams; i++)
        {
            audioFiles.Add($"\"{nf}.{i}.WAV\"");
        }

        ffmpegCommand += string.Join(" -i ", audioFiles);
        ffmpegCommand += " -map 0";
        for (var i = 1; i < streams; i++)
        {
            ffmpegCommand += $" -map {i}:a:0";
        }

        if (CropAlpha)
        {
            ffmpegCommand += " -vf \"crop=256:256:0:256";
        }
        if (CropRgb)
        {
            ffmpegCommand += " -vf \"crop=256:256:0:0";
        }
        if (ffmpegCommand.Contains("-vf") && (ScaleFactor == 1)) ffmpegCommand += "\"";

        if (ScaleFactor != 1)
        {
            if (!ffmpegCommand.Contains("-vf"))
            {
                ffmpegCommand += " -vf \"";
            }
            ffmpegCommand += $"scale=iw*{ScaleFactor}:ih*{ScaleFactor}\" -sws_flags neighbor";
        }

        ffmpegCommand += $" -c:v libx264 -crf 3 -preset slow -shortest \"{Output}\"";
        StaticUtils.ProcessFFmpeg(FFmpegPath, ffmpegCommand);
        File.Delete(nf + ".TEMP.M2V");
        for (var i = 1; i <= streams; i++)
        {
            File.Delete(nf + $".{i}.WAV");
            File.Delete(nf + $".{i}.INT");
        }

        File.Delete(nf + ".IPU");
        Console.WriteLine($"\rFile saved as {Output}");
    }
}