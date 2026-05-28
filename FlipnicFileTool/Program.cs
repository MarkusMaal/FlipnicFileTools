using System.Diagnostics;
using FlipnicFileTool.Tools;
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool;

internal static class Program
{
    private static readonly Config Cfg = new();
    
    public static int Main(string[] args)
    {
        try
        {
            Cfg.LoadFromArgs(args);
            if (Cfg.Mode == Enums.Modes.Quit)
            {
                return 0;
            }

            Cfg.ShowSignature();
            var code = Cfg.DetectAndDisplayErrors();
            if (code != -1) return code;

            switch (Cfg.Mode)
            {
                case Enums.Modes.NotImplemented:
                    StaticUtils.DecodeColors("~-CError~--: This file format cannot be parsed by this version of Flipnic File Tools. Support will be added in a future version.");
                    Console.WriteLine();
                    return 9;
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
                case Enums.Modes.ConvertIcoTexture:
                case Enums.Modes.ConvertIcoObj:
                    _ = new IcoTools(Cfg);
                    break;
                case Enums.Modes.ListResources:
                case Enums.Modes.ShowSstToc:
                case Enums.Modes.ShowPseudoCode:
                case Enums.Modes.ShowGimmick:
                case Enums.Modes.ShowCameras:
                case Enums.Modes.SstResize:
                    _ = new SstTools(Cfg);
                    break;
                case Enums.Modes.ShowFpc:
                case Enums.Modes.ConvertXml:
                case Enums.Modes.ConvertFpc:
                case Enums.Modes.GenerateAnimation:
                    _ = new CameraTools(Cfg);
                    break;
                case Enums.Modes.ShowMessages:
                case Enums.Modes.GenerateMsg:
                    _ = new MsgTools(Cfg);
                    break;
                case Enums.Modes.ListBin:
                case Enums.Modes.ExtractBin:
                case Enums.Modes.ReplaceBin:
                case Enums.Modes.ExtractPak:
                case Enums.Modes.ListPak:
                case Enums.Modes.ReplacePak:
                    _ = new BinTools(Cfg);
                    break;
                case Enums.Modes.ShowHd:
                case Enums.Modes.ShowBd:
                case Enums.Modes.ExtractSamples:
                case Enums.Modes.ShowMidi:
                case Enums.Modes.ConvertSf2:
                case Enums.Modes.ConvertInt:
                case Enums.Modes.ConvertSvag:
                    _ = new AudioTools(Cfg);
                    break;
                case Enums.Modes.ShowHelp:
                    GetHelp();
                    break;
                case Enums.Modes.ShowLp4:
                case Enums.Modes.ExportObj:
                case Enums.Modes.ShowCol:
                case Enums.Modes.ExportColObj:
                case Enums.Modes.ExportBbox:
                    _ = new ModelTools(Cfg);
                    break;
                case Enums.Modes.ShowTim2:
                case Enums.Modes.ConvertTim2:
                case Enums.Modes.ShowMlb:
                case Enums.Modes.GenerateMockup:
                    _ = new ImageTools(Cfg);
                    break;
                case Enums.Modes.ConvertIpu:
                case Enums.Modes.ListPssStreams:
                case Enums.Modes.ExtractPssStreams:
                case Enums.Modes.ShowIpu:
                case Enums.Modes.ConvertPssMpeg:
                case Enums.Modes.GeneratePss:
                case Enums.Modes.IpuFix:
                    _ = new VideoTools(Cfg);
                    break;
                case Enums.Modes.ShowLay:
                    Console.Write(new FpnLay(File.ReadAllBytes(Cfg.FileName)).ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.ShowVsd:
                    var vsd = new FpnVsd(File.OpenRead(Cfg.FileName));
                    Console.WriteLine($"Vibration Strength Data\n{vsd.ToString(StaticUtils.SimpleOutput)}");
                    break;
                case Enums.Modes.ShowFpd:
                    var fpd = new FpnFpd(File.OpenRead(Cfg.FileName));
                    Console.WriteLine(fpd);
                    break;
                case Enums.Modes.ExportFpdObj:
                    StaticUtils.ExportObj(Cfg.Output, new FpnFpd(File.OpenRead(Cfg.FileName)).DrawPath(), null);
                    break;
                case Enums.Modes.ShowIso:
                case Enums.Modes.ExtractIso:
                case Enums.Modes.ReplaceIso:
                    _ = new IsoTools(Cfg);
                    break;
                case Enums.Modes.ShowLit:
                    var lit = new FpnLit(File.OpenRead(Cfg.FileName));
                    Console.Write(lit.ToString(StaticUtils.SimpleOutput));
                    break;
                case Enums.Modes.Quit:
                    break;
                case Enums.Modes.ShowVss:
                    var vss = new VssVer(File.ReadAllBytes(Cfg.FileName));
                    Console.WriteLine(vss);
                    break;
                case Enums.Modes.ShowFtl:
                    var ftl = new FpnTexList(File.OpenRead(Cfg.FileName));
                    Console.WriteLine(ftl);
                    break;
                case Enums.Modes.ShowElf:
                    var elf = new Game(File.OpenRead(Cfg.FileName));
                    Console.WriteLine(elf);
                    break;
                case Enums.Modes.ShowDummy:
                    var df = new Dummy(File.OpenRead(Cfg.FileName));
                    Console.WriteLine(df);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args));
            }

            return 0;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            if (Cfg.Test) throw;
            var dt = new DebugTools(e);
            var exitCode = dt.Inspector();
            if (exitCode == -1) throw;
            return exitCode;
        }
    }

    public static void GetDisclaimer()
    {
        StaticUtils.DecodeColors("~-4Disclaimer~--: ");
        Console.WriteLine(StaticUtils.DisclaimerText);
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