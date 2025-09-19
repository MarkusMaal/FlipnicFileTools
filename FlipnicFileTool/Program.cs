using System.Diagnostics;
using FlipnicLib;
using FlipnicLib.Jam;
using FlipnicLib.Midi;
using Syroot.BinaryData;

namespace FlipnicFileTool;

internal static class Program
{

    private static bool _grayscale;
    private static string _mlbSect = "";
    private static string _magickPath = "magick";
    private static string _fFmpegPath = "ffmpeg";
    private static bool _usePng;
    private static string _midiFile = "";
    private static string _bdFile = "";
    private static string FileName { get; set; } = "";
    
    public static void Main(string[] args)
    {
        var secondaryFileName = "";
        var outFile = ".";
        var lastPar = "";
        var mode = Enums.Modes.ShowHelp;
        if (args.Length > 0 && File.Exists(args[0]))
        {
            mode = Enums.GuessAction(args[0]);
            if (mode != Enums.Modes.ShowHelp)
            {
                FileName = args[0];
            }
        }
        foreach (var arg in args)
        {
            mode = Enums.GetMode(arg, mode);
            switch (arg)
            {
                case "--simple":
                    StaticUtils.SimpleOutput = true;
                    break;
                case "--low-memory":
                    StaticUtils.LowMem = true;
                    break;
                case "--grayscale":
                    _grayscale = true;
                    break;
                case "--pal":
                    StaticUtils.Pal = true;
                    break;
                case "--png":
                    _usePng = true;
                    break;
            }

            switch (lastPar)
            {
                case "--show-gimmick":
                    secondaryFileName = arg;
                    break;
                case "--mlb-section":
                    _mlbSect = arg;
                    break;
                case "--input":
                    FileName = arg;
                    break;
                case "--output":
                    outFile = arg;
                    break;
                case "--magick-path":
                    _magickPath = arg;
                    break;
                case "--ffmpeg-path":
                    _fFmpegPath = arg;
                    break;
                case "--msg-path":
                    StaticUtils.MsgFile = arg;
                    break;
                case "--no-envelopes":
                    StaticUtils.ExportEnvelopes = false;
                    break;
                case "--midi-file":
                    _midiFile = arg;
                    break;
                case "--bd-file":
                    _bdFile = arg;
                    break;
                case "--no-velocity":
                    StaticUtils.ExportVelocity = false;
                    break;
                default:
                    break;
            }
            lastPar = arg;
        }

        if (FileName == "" && mode != Enums.Modes.ShowHelp)
        {
            Console.WriteLine("Must specify input FileName in this case!");
            return;
        }
        if (!File.Exists(FileName) && mode != Enums.Modes.ShowHelp)
        {
            Console.WriteLine("Error: Input file does not exist!");
            return;
        }

        if (mode != Enums.Modes.ShowHelp && new FileInfo(FileName).IsReadOnly && outFile != "")
        {
            Console.WriteLine("Error: Read-only file system");
            return;
        }
        
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (mode)
        {
            case Enums.Modes.ListResources:
                Console.Write(new FpnSst(File.OpenRead(FileName)).GenerateMagicNumbers());
                break;
            case Enums.Modes.ShowFpc:
                Console.Write(new FpnFpc(FileName).ToString());
                break;
            case Enums.Modes.ConvertXml:
                new FpnFpc(FileName).GenerateXML().Save(outFile);
                break;
            case Enums.Modes.ShowSstToc:
                Console.Write(new FpnSst(File.OpenRead(FileName)).ListEntries());
                break;
            case Enums.Modes.ShowPseudoCode:
                Console.Write(new FpnSst(File.OpenRead(FileName)).GeneratePseudoCode());
                break;
            case Enums.Modes.ShowGimmick:
                new FpnSst(File.OpenRead(FileName)).ShowGimmick(secondaryFileName);
                break;
            case Enums.Modes.ShowMessages:
                Console.WriteLine(StaticUtils.SimpleOutput ? new FpnMsg(FileName).ToSimpleString() : new FpnMsg(FileName).ToString());
                break;
            case Enums.Modes.ListPssStreams:
                Console.WriteLine(new Pss(FileName).ListPss(File.OpenRead(FileName)));
                break;
            case Enums.Modes.ExtractPssStreams:
                new Pss(FileName).ListPss(File.OpenRead(FileName), true, outFile);
                break;
            case Enums.Modes.ListBin:
                new BinFile().ListBin(File.OpenRead(FileName));
                break;
            case Enums.Modes.ExtractBin:
                new BinFile().ExtractBin(FileName, outFile);
                break;
            case Enums.Modes.ShowHd:
                var jh = new JamHeader();
                jh.Read(new BinaryStream(new FileStream(FileName, FileMode.Open, FileAccess.Read)));
                Console.Write(jh.ToString());
                break;
            case Enums.Modes.ShowMidi:
                var midi = new Midi(FileName);
                midi.Read();
                Console.Write(midi.ToString());
                break;
            case Enums.Modes.ShowHelp:
                GetHelp();
                break;
            case Enums.Modes.ShowLp4:
                Console.WriteLine(new Lp4(File.ReadAllBytes(FileName), FileName).ToString());
                break;
            case Enums.Modes.ShowMlb:
                Console.WriteLine(new FpnMlb(File.ReadAllBytes(FileName)).ToString());
                break;
            case Enums.Modes.ConvertTim2:
                var texture = new Tim2(File.ReadAllBytes(FileName), FileName, _grayscale);
                if (_usePng)
                {
                    texture.SavePng(new FileStream(outFile,  FileMode.Create));
                    break;
                }
                var fs = new FileStream(outFile, FileMode.Create);
                texture.SaveBitmap(fs);
                break;
            case Enums.Modes.ConvertIpu:
                Ipu.IpuConvert(FileName, outFile, _fFmpegPath);
                break;
            case Enums.Modes.ConvertSf2:
                Converter.InstrumentToSoundFont2(_midiFile != "" ? _midiFile : (FileName[..^3] + ".MID"), FileName, _bdFile != "" ? _bdFile : (FileName[..^2] + "BD"), outFile);
                break;
            case Enums.Modes.ConvertInt:
                StaticUtils.ConvertAudio(outFile, FileName);
                break;
            case Enums.Modes.ConvertSvag:
                StaticUtils.ConvertAudio(outFile, FileName, true);
                Console.WriteLine($"File saved as {outFile}");
                break;
            case Enums.Modes.ShowVsd:
                var vsd = new FpnVsd(File.OpenRead(FileName));
                Console.WriteLine($"Vibration Strength Data\n{vsd}");
                break;
            case Enums.Modes.ConvertPssMov:
                new Pss(FileName).ListPss(File.OpenRead(FileName), true, new FileInfo(outFile).Directory!.FullName);
                var nf = Path.Combine(new FileInfo(outFile).Directory!.FullName, new FileInfo(FileName).Name);
                Ipu.IpuConvert(nf + ".IPU", nf + ".TEMP.MOV", _fFmpegPath);
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

                var ffmpegCommand = $"-i \"{nf}.TEMP.MOV\" -i ";
                List<string> audioFiles = [];
                for (var i = 1; i < streams; i++)
                {
                    audioFiles.Add($"\"{nf}.{i}.WAV\"");
                }
                ffmpegCommand += string.Join(" -i ", audioFiles);
                ffmpegCommand += " -map 0";
                for (var i = 1; i < streams; i++)
                {
                    ffmpegCommand += $" -map {i}:a";
                }
                ffmpegCommand += $" -c:v copy -shortest \"{outFile}\"";
                StaticUtils.ProcessFFmpeg(_fFmpegPath, ffmpegCommand);
                File.Delete(nf + ".TEMP.MOV");
                for (var i = 1; i <= streams; i++)
                {
                    File.Delete(nf + $".{i}.WAV");
                    File.Delete(nf + $".{i}.INT");
                }
                File.Delete(nf + ".IPU");
                Console.WriteLine($"\rFile saved as {outFile}");
                break;
            case Enums.Modes.ShowTim2:
                Console.WriteLine(new Tim2(File.ReadAllBytes(FileName), FileName).ToString());
                break;
            case Enums.Modes.ShowLay:
                Console.Write(new FpnLay(File.ReadAllBytes(FileName)));
                break;
            case Enums.Modes.GenerateMockup:
                StaticUtils.GenerateEmptyPng(outFile + "_", 640, StaticUtils.Pal ? 512 : 480);
                var root = new FileInfo(FileName).Directory?.FullName ?? ".";
                var magickCommand = $"\"{outFile}_\" ";
                foreach (var sect in new FpnMlb(File.ReadAllBytes(FileName)).Sections.Where(me => (_mlbSect == "") || (me.Key == _mlbSect)).SelectMany(me => me.Value))
                {
                    try
                    {
                        var textureFile = sect.Texture.Split('\\')[^1].ToUpper();
                        new Tim2(File.ReadAllBytes(Path.Combine(root, textureFile)), Path.Combine(root, textureFile), _grayscale).SavePng(
                            new FileStream(Path.Combine(root, textureFile.Replace(".TM2", ".TEMP.PNG")), FileMode.Create));

                        magickCommand +=
                            $" ( \"{Path.Combine(root, textureFile.Replace(".TM2", ".TEMP.PNG"))}\" ) -geometry +{sect.PosX}+{sect.PosY} -composite ";
                    }
                    catch
                    {
                        // ignored
                    }
                }
                magickCommand += $" \"{outFile}\"";
                Console.WriteLine($"Executing shell command: magick {magickCommand}");
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _magickPath,
                        Arguments = magickCommand.Replace("+−", "+"),
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                p.Start();
                p.WaitForExit();
                File.Delete(outFile + "_");
                foreach (var f in new FileInfo(outFile).Directory!.GetFiles())
                {
                    if (f.Name.EndsWith(".TEMP"))
                    {
                        f.Delete();
                    }
                }
                break;
        }
    }
    private static void GetHelp()
    {
        var ds = (OperatingSystem.IsWindows() ? "" : "./");
        StaticUtils.DecodeColors($"""
                                  ~-BFlipnic File Tools {StaticUtils.DotFloatString(StaticUtils.LibVersion)}~-- [~-ECLI Usage~--]
                                  ~-7{ds + Process.GetCurrentProcess().ProcessName} [filename] [options]~--

                                  """);
        Help.HelpUtils.GenerateHelp();
        StaticUtils.DecodeColors("~-7* ~-FDefault action");
    }
}