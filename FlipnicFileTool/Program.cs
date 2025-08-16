using System.Diagnostics;

namespace FlipnicFileTool;

internal static class Program
{
    private enum Modes : int
    {
        ListResources,
        ShowFpc,
        ConvertXml,
        ShowHelp,
        ShowSstToc,
        ShowMessages,
        ListPssStreams,
        ExtractPssStreams,
        ListBin,
        ExtractBin,
        ShowGimmick,
        ShowLp4,
        ShowMlb,
        ShowTim2,
        ConvertTim2
    }

    public static bool SimpleOutput = false;
    public static bool LowMem = false;
    private static string FileName = "";
    
    public static void Main(string[] args)
    {
        var secondaryFileName = "";
        var outFile = "";
        var lastPar = "";
        var mode = Modes.ShowHelp;
        if (args.Length > 0 && File.Exists(args[0]))
        {
            mode = GuessAction(args[0]);
            if (mode != Modes.ShowHelp)
            {
                FileName = args[0];
            }
        }
        foreach (var arg in args)
        {
            mode = arg switch
            {
                "--help" => Modes.ShowHelp,
                "--show-fpc" => Modes.ShowFpc,
                "--show-sst-resources" => Modes.ListResources,
                "--convert-fpc-to-xml" => Modes.ConvertXml,
                "--show-sst-toc" => Modes.ShowSstToc,
                "--show-messages" => Modes.ShowMessages,
                "--list-pss-streams" => Modes.ListPssStreams,
                "--extract-pss-streams" => Modes.ExtractPssStreams,
                "--list-files" => Modes.ListBin,
                "--extract-files" => Modes.ExtractBin,
                "--show-gimmick" => Modes.ShowGimmick,
                "--show-lp4" => Modes.ShowLp4,
                "--show-mlb" => Modes.ShowMlb,
                "--show-tim2" => Modes.ShowTim2,
                "--convert-tim2" => Modes.ConvertTim2,
                _ => mode
            };
            switch (arg)
            {
                case "--simple":
                    SimpleOutput = true;
                    break;
                case "--low-memory":
                    LowMem = true;
                    break;
            }

            switch (lastPar)
            {
                case "--show-gimmick":
                    secondaryFileName = arg;
                    break;
                case "--input":
                    FileName = arg;
                    break;
                case "--output":
                    outFile = arg;
                    break;
                default:
                    break;
            }
            lastPar = arg;
        }

        if (FileName == "" && mode != Modes.ShowHelp)
        {
            Console.WriteLine("Must specify input FileName in this case!");
            return;
        }
        
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (mode)
        {
            case Modes.ListResources:
                new FpnSst(FileName).GenerateMagicNumbers();
                break;
            case Modes.ShowFpc:
                Console.Write(new FpnFpc(FileName).ToString());
                break;
            case Modes.ConvertXml:
                new FpnFpc(FileName).GenerateXML().Save(outFile);
                break;
            case Modes.ShowSstToc:
                new FpnSst(FileName).ListEntries();
                break;
            case Modes.ShowGimmick:
                new FpnSst(FileName).ShowGimmick(secondaryFileName);
                break;
            case Modes.ShowMessages:
                Console.WriteLine(SimpleOutput ? new FpnMsg(FileName).ToSimpleString() : new FpnMsg(FileName).ToString());
                break;
            case Modes.ListPssStreams:
                Pss.ListPss(FileName);
                break;
            case Modes.ExtractPssStreams:
                Pss.ListPss(FileName, true, outFile);
                break;
            case Modes.ListBin:
                BinFile.ListBin(FileName);
                break;
            case Modes.ExtractBin:
                BinFile.ExtractBin(FileName, outFile);
                break;
            case Modes.ShowHelp:
                Console.WriteLine(GetHelp());
                break;
            case Modes.ShowLp4:
                Console.WriteLine(new Lp4(File.ReadAllBytes(FileName)).ToString());
                break;
            case Modes.ShowMlb:
                Console.WriteLine(new FpnMlb(File.ReadAllBytes(FileName)).ToString());
                break;
            case Modes.ConvertTim2:
                new Tim2(File.ReadAllBytes(FileName)).SaveBitmap(outFile);
                break;
            case Modes.ShowTim2:
                Console.WriteLine(new Tim2(File.ReadAllBytes(FileName)).ToString());
                break;
        }
    }

    private static string GetHelp()
    {
        return $"""
               Usage: {Process.GetCurrentProcess().ProcessName} [FileName] [options]
               
               Specifying a FileName without any option will run an action corresponding file format below highlighted with an asterisk (*).
               
               --input                    File to open
               --output                   File to write to
               --help                     Display help
               --simple                   Use output that is easy to parse for computer programs
               --low-memory               Reduces performance to save on memory usage
               
               Flipnic Camera sequences (*.FPC)
               
               --show-fpc*                Display data from .FPC file as human-readable text
               --convert-fpc-to-xml       Convert .FPC file to .XML

               Stage information files (*.SST)
               
               --show-sst-resources       Display all resources referenced by SST file
               --show-sst-toc*            Display table of contents of the SST file
               --show-gimmick [name]      Display a gimmick (name from TOC)
               
               Message file (JA.MSG)
               
               --show-messages*           Display all strings stored in the file
               
               Interleaved audio/video stream (*.PSS)
               
               --list-pss-streams*        List all available streams in a .PSS file
               --extract-pss-streams      Demux a .PSS file to .IPU and .INT files (output = folder)
               
               Blob files (*.BIN)
               
               --list-files*              List all files inside this container file
               --extract-files            Extract files inside the container to a folder (output = folder)
               
               Resource files (*.LP4)
               
               --show-lp4*                Display general information about the file
               
               Menu files (*.MLB)
               
               --show-mlb*                Display all menu elements as a table
               
               Texture files (*.TM2)
               
               --show-tim2*               Display information about a texture file
               --convert-tim2             Converts a texture file to a bitmap (.BMP file)
               """;
    }

    private static Modes GuessAction(string FileName)
    {
        return Path.GetExtension(FileName) switch
        {
            ".FPC" => Modes.ShowFpc,
            ".SST" => Modes.ShowSstToc,
            ".MSG" => Modes.ShowMessages,
            ".PSS" => Modes.ListPssStreams,
            ".BIN" => Modes.ListBin,
            ".LP4" => Modes.ShowLp4,
            ".MLB" => Modes.ShowMlb,
            ".TM2" => Modes.ShowTim2,
            _ => Modes.ShowHelp
        };
    }

    public static string GetFileName()
    {
        return FileName;
    }
}