using System.Diagnostics;
using FlipnicLib;
using FlipnicLib.Formats;
using FlipnicLib.Formats.Jam;
using FlipnicLib.Formats.Midi;
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
    
    public static int Main(string[] args)
    {
        try
        {
            var secondaryFileName = "";
            var outFile = ".";
            var lastPar = "";
            var mode = Enums.Modes.NoAction;
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
                    case "--version":
                        Console.WriteLine(StaticUtils.DotFloatString(StaticUtils.LibVersion));
                        return 0;
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

            if (args.Length > 0 && File.Exists(args[0]))
            {
                if (mode == Enums.Modes.ShowHelp)
                {
                    mode = Enums.GuessAction(args[0]);
                    if (mode != Enums.Modes.ShowHelp)
                    {
                        FileName = args[0];
                    }
                }
            }

            if (!StaticUtils.SimpleOutput)
            {
                StaticUtils.DecodeColors(
                    $"~-BFlipnic File Tools {StaticUtils.DotFloatString(StaticUtils.LibVersion)}~--\n");
            }

            if (FileName == "" && mode != Enums.Modes.ShowHelp)
            {
                StaticUtils.DecodeColors(
                    "~-CError~--: Must specify input filename in this case! To see command line usage, append the ~-F--help~-- flag.");
                Console.WriteLine();
                return 1;
            }

            if (!File.Exists(FileName) && mode != Enums.Modes.ShowHelp)
            {
                StaticUtils.DecodeColors("~-CError~--: Input file does not exist!");
                Console.WriteLine();
                return 2;
            }

            if (mode != Enums.Modes.ShowHelp && new FileInfo(FileName).IsReadOnly && outFile != "")
            {
                StaticUtils.DecodeColors("~-CError~--: Read-only file system");
                Console.WriteLine();
                return 3;
            }

            if (FileName != "" && !StaticUtils.SimpleOutput)
            {
                Console.WriteLine($"Filename: {FileName}");
                Console.Write("\n");
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (mode)
            {
                case Enums.Modes.ConflictingModes:
                    StaticUtils.DecodeColors("~-CError~--: Conflicting arguments detected. Please check syntax!");
                    Console.WriteLine();
                    return 4;
                case Enums.Modes.NoAction:
                    StaticUtils.DecodeColors(
                        "~-CError~--: Syntax is incorrect. To see command line usage, append the ~-F--help~-- flag.");
                    Console.WriteLine();
                    return 5;
                case Enums.Modes.ShowIco:
                    var ico = new SaveIcon(File.ReadAllBytes(FileName));
                    ico.Read();
                    Console.WriteLine(ico.ToString());
                    break;
                case Enums.Modes.ConvertIcoTexture:
                    ico = new SaveIcon(File.ReadAllBytes(FileName));
                    ico.Read();
                    ico.Texture?.SavePng(File.OpenWrite(outFile));
                    break;
                case Enums.Modes.ConvertIcoObj:
                    ico = new SaveIcon(File.ReadAllBytes(FileName));
                    ico.Read();
                    List<float> modelData = [];
                    foreach (var vertex in ico.Vertices)
                    {
                        modelData.Add(vertex.TextureX / 4096f);
                        modelData.Add(-vertex.TextureY / 4096f);
                        modelData.Add(vertex.CoordX / 4096f);
                        modelData.Add(-vertex.CoordY / 4096f);
                        modelData.Add(-vertex.CoordZ / 4096f);
                    }
                    StaticUtils.ExportObj(outFile, modelData.ToArray(), ico.Texture);
                    break;
                case Enums.Modes.ListResources:
                    Console.Write(new FpnSst(File.OpenRead(FileName)).GenerateMagicNumbers());
                    break;
                case Enums.Modes.ShowFpc:
                    Console.Write(new FpnFpc(FileName).ToString(StaticUtils.SimpleOutput));
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
                    Console.WriteLine(StaticUtils.SimpleOutput
                        ? new FpnMsg(FileName).ToSimpleString()
                        : new FpnMsg(FileName).ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.GenerateMsg:
                    Console.WriteLine("Loading text file...");
                    var lines = File.ReadAllLines(FileName);
                    var msg = new FpnMsg
                    {
                        Messages = lines
                    };
                    Console.WriteLine("Saving message file...");
                    File.WriteAllBytes(outFile, msg.GetData());
                    Console.WriteLine($"File saved as {outFile}");
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
                    new BinFile().ExtractBin(File.OpenRead(FileName), outFile);
                    break;
                case Enums.Modes.ShowHd:
                    var jh = new JamHeader();
                    jh.Read(new BinaryStream(new FileStream(FileName, FileMode.Open, FileAccess.Read)));
                    Console.Write(jh.ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.ShowMidi:
                    var midi = new Midi(FileName);
                    midi.Read();
                    Console.Write(midi.ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.ShowHelp:
                    GetHelp();
                    break;
                case Enums.Modes.ShowLp4:
                    var lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
                    lp4.Read();
                    Console.WriteLine(lp4.ToString());
                    break;
                case Enums.Modes.ExportObj:
                    lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
                    lp4.Read();
                    StaticUtils.ExportObj(outFile, lp4.GetVerticies(), lp4.Texture);
                    break;
                case Enums.Modes.ShowMlb:
                    Console.WriteLine(new FpnMlb(File.ReadAllBytes(FileName)).ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.ConvertTim2:
                    var texture = new Tim2(File.ReadAllBytes(FileName), FileName, _grayscale);
                    if (_usePng)
                    {
                        texture.SavePng(new FileStream(outFile, FileMode.Create));
                        break;
                    }

                    var fs = new FileStream(outFile, FileMode.Create);
                    texture.SaveBitmap(fs);
                    break;
                case Enums.Modes.ConvertIpu:
                    Ipu.IpuConvert(FileName, outFile, _fFmpegPath);
                    break;
                case Enums.Modes.ConvertSf2:
                    Converter.InstrumentToSoundFont2(_midiFile != "" ? _midiFile : (FileName[..^3] + ".MID"), FileName,
                        _bdFile != "" ? _bdFile : (FileName[..^2] + "BD"), outFile);
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
                    Console.WriteLine($"Vibration Strength Data\n{vsd.ToString(StaticUtils.SimpleOutput)}");
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
                    Console.WriteLine(
                        new Tim2(File.ReadAllBytes(FileName), FileName).ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.ShowLay:
                    Console.Write(new FpnLay(File.ReadAllBytes(FileName)).ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.GenerateMockup:
                    StaticUtils.GenerateEmptyPng(outFile + "_", 640, StaticUtils.Pal ? 512 : 480);
                    var root = new FileInfo(FileName).Directory?.FullName ?? ".";
                    var magickCommand = $"\"{outFile}_\" ";
                    foreach (var sect in new FpnMlb(File.ReadAllBytes(FileName)).Sections
                                 .Where(me => (_mlbSect == "") || (me.Key == _mlbSect)).SelectMany(me => me.Value))
                    {
                        try
                        {
                            var textureFile = sect.Texture.Split('\\')[^1].ToUpper();
                            new Tim2(File.ReadAllBytes(Path.Combine(root, textureFile)),
                                Path.Combine(root, textureFile), _grayscale).SavePng(
                                new FileStream(Path.Combine(root, textureFile.Replace(".TM2", ".TEMP.PNG")),
                                    FileMode.Create));

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
                case Enums.Modes.ShowIpu:
                    Console.WriteLine(Ipu.GetInfoAsString(File.OpenRead(FileName)));
                    break;
            }

            return 0;
        }
        catch (Exception e)
        {
            var indentedTrace = "";
            if (e.StackTrace != null) indentedTrace = "  " + string.Join("\n     ", e.StackTrace.Split("\n"));
            Console.Clear();
            StaticUtils.DecodeColors($"""
                                     ~-C
                                     Unhandled fatal exception~--
                                     This program has been halted due to a critical error. If this keeps happening, it may be a bug and should be reported to the developer!
                                     
                                     Context:
                                        Executable: {Process.GetCurrentProcess().ProcessName}
                                        CLI arguments: {Environment.CommandLine}
                                        Global variables:
                                           Simple output: {StaticUtils.SimpleOutput}
                                           Export velocity: {StaticUtils.ExportVelocity}
                                           Export envelopes: {StaticUtils.ExportEnvelopes}
                                           Is mode set: {StaticUtils.IsModeSet}
                                           Low memory: {StaticUtils.LowMem}
                                           Alt. SF2 method: {StaticUtils.AltSf2Method}
                                           Live load status: {StaticUtils.LiveLoadStatus}
                                           Message file: {StaticUtils.MsgFile}
                                           PAL: {StaticUtils.Pal}
                                           Load index: {StaticUtils.LoadIdx}
                                           Window width: {StaticUtils.WindowWidth}
                                     
                                     Environment:
                                        FlipnicLib version: {StaticUtils.DotFloatString(StaticUtils.LibVersion)}
                                        Microsoft .NET version: {Environment.Version}
                                        Operating system: {Environment.OSVersion}
                                        Working directory: {Environment.CurrentDirectory}
                                        Memory allocation: {StaticUtils.GetFilesizeString(Environment.WorkingSet)}
                                        Page file: {StaticUtils.GetFilesizeString(Environment.SystemPageSize)}
                                        CPU time: {Environment.CpuUsage.TotalTime}
                                        System shutting down: {Environment.HasShutdownStarted}
                                     
                                     Technical info:
                                        {e.Message}
                                        {indentedTrace}
                                     """);
            if (Debugger.IsAttached) throw;
            Console.Write(
                "\n\nWe couldn't auto-detect a debugger being attached, but if you wish, you can still throw this exception.\n\nPressing Y will throw this exception to a JIT debugger\nPressing N will quit the application with an exit code\n\n[Y/N] ");
            while (true)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine();
                        throw;
                    case ConsoleKey.N:
                        Console.WriteLine();
                        return 255;
                    default:
                        continue;
                }
            }
            throw;
        }
    }
    private static void GetHelp()
    {
        var ds = (OperatingSystem.IsWindows() ? "" : "./");
        StaticUtils.DecodeColors($"""
                                  ~-EUsage~-- ~-7{ds + Process.GetCurrentProcess().ProcessName} [filename] [options]~--

                                  """);
        Help.HelpUtils.GenerateHelp();
        StaticUtils.DecodeColors("~-7* ~-FDefault action\n");
    }
}