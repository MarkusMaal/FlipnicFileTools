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
        ShowLp4
    }

    public static bool SimpleOutput = false;
    public static bool LowMem = false;
    
    public static void Main(string[] args)
    {
        var fileName = "";
        var secondaryFileName = "";
        var outFile = "";
        var lastPar = "";
        var mode = Modes.ShowHelp;
        if (args.Length > 0 && File.Exists(args[0]))
        {
            mode = GuessAction(args[0]);
            if (mode != Modes.ShowHelp)
            {
                fileName = args[0];
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
                    fileName = arg;
                    break;
                case "--output":
                    outFile = arg;
                    break;
                default:
                    break;
            }
            lastPar = arg;
        }

        if (fileName == "" && mode != Modes.ShowHelp)
        {
            Console.WriteLine("Must specify input filename in this case!");
            return;
        }
        
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (mode)
        {
            case Modes.ListResources:
                new FpnSst(fileName).GenerateMagicNumbers();
                break;
            case Modes.ShowFpc:
                Console.Write(new FpnFpc(fileName).ToString());
                break;
            case Modes.ConvertXml:
                new FpnFpc(fileName).GenerateXML().Save(outFile);
                break;
            case Modes.ShowSstToc:
                new FpnSst(fileName).ListEntries();
                break;
            case Modes.ShowGimmick:
                new FpnSst(fileName).ShowGimmick(secondaryFileName);
                break;
            case Modes.ShowMessages:
                Console.WriteLine(SimpleOutput ? new FpnMsg(fileName).ToSimpleString() : new FpnMsg(fileName).ToString());
                break;
            case Modes.ListPssStreams:
                Pss.ListPss(fileName);
                break;
            case Modes.ExtractPssStreams:
                Pss.ListPss(fileName, true, outFile);
                break;
            case Modes.ListBin:
                BinFile.ListBin(fileName);
                break;
            case Modes.ExtractBin:
                BinFile.ExtractBin(fileName, outFile);
                break;
            case Modes.ShowHelp:
                Console.WriteLine(GetHelp());
                break;
            case Modes.ShowLp4:
                Console.WriteLine(new Lp4(File.ReadAllBytes(fileName)).ToString());
                break;
        }
    }

    private static string GetHelp()
    {
        return $"""
               Usage: {Process.GetCurrentProcess().ProcessName} [filename] [options]
               
               Specifying a filename without any option will run an action corresponding file format below highlighted with an asterisk (*).
               
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
               
               --show-lp4                 Display general information about the file
               """;
    }

    private static Modes GuessAction(string fileName)
    {
        return Path.GetExtension(fileName) switch
        {
            ".FPC" => Modes.ShowFpc,
            ".SST" => Modes.ShowSstToc,
            ".MSG" => Modes.ShowMessages,
            ".PSS" => Modes.ListPssStreams,
            ".BIN" => Modes.ListBin,
            ".LP4" => Modes.ShowLp4,
            _ => Modes.ShowHelp
        };
    }
}