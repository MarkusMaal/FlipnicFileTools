using System.Globalization;
using FlipnicLib;

namespace FlipnicFileTool;

public class Config
{
    /// <summary>
    /// Input filename
    /// </summary>
    public string FileName { get; set; } = "";
    
    /// <summary>
    /// Output filename
    /// </summary>
    public string Output { get; set; } = ".";
    
    /// <summary>
    /// Secondary input filename
    /// </summary>
    public string SecondaryFileName { get; set; } = "";

    /// <summary>
    /// Mode of operation for the app
    /// </summary>
    public Enums.Modes Mode { get; set; } = Enums.Modes.NoAction;
    
    /// <summary>
    /// Path to ImageMagick executable
    /// </summary>
    public string MagickPath { get; set; } = "magick";
    
    /// <summary>
    /// Path to FFmpeg executable
    /// </summary>
    public string FFmpegPath { get; set; } = "ffmpeg";
    
    /// <summary>
    /// Section name of a .MLB file
    /// </summary>
    public string MlbSect { get; set; } = "";
    
    /// <summary>
    /// Path to a .MID file (when doing soundfont conversions)
    /// </summary>
    public string MidiFile { get; set; } = "";
    
    /// <summary>
    /// Path to a .BD file (when doing soundfont conversions)
    /// </summary>
    public string BdFile { get; set; } = "";
    
    /// <summary>
    /// Name of a virtual file inside a .BIN/.ISO container
    /// </summary>
    public string VFile { get; set; } = "";
    
    /// <summary>
    /// Remove Alpha information from low-res FMVs (conversion)
    /// </summary>
    public bool CropAlpha { get; set; }
    
    
    /// <summary>
    /// Remove RGB information from low-res FMVs (conversion)
    /// </summary>
    public bool CropRgb { get; set; }
    
    /// <summary>
    /// How many times to scale up the resolution (FMV conversion)
    /// </summary>
    public int ScaleFactor { get; set; } = 1;
    
    /// <summary>
    /// Enable testing mode
    /// </summary>
    public bool Test { get; set; } = false;
    
    /// <summary>
    /// Do we create a .WAV file in addition to the .SF2 file?
    /// </summary>
    public bool SynthesizeWav { get; set; } = false;

    /// <summary>
    /// Set configuration based on the args specified by the user
    /// </summary>
    /// <param name="args">List of args received from the command line</param>
    public void LoadFromArgs(string[] args)
    {
        var lastPar = "";
        foreach (var arg in args)
        {
            Mode = Enums.GetMode(arg, Mode);
            switch (arg)
            {
                case "--simple":
                    StaticUtils.SimpleOutput = true;
                    break;
                case "--low-memory":
                    StaticUtils.LowMem = true;
                    break;
                case "--pal":
                    StaticUtils.Pal = true;
                    break;
                case "--crop-rgb":
                    CropRgb = true;
                    break;
                case "--crop-alpha":
                    CropAlpha = true;
                    break;
                case "--alternate-normals":
                    StaticUtils.AlternateNormals = true;
                    break;
                case "--test":
                    Test = true;
                    break;
                case "--synthesize-wav":
                    SynthesizeWav = true;
                    break;
                case "--version":
                    Console.WriteLine(StaticUtils.DotFloatString(StaticUtils.LibVersion) + (StaticUtils.IsBeta ? " BETA" : ""));
                    Mode = Enums.Modes.Quit;
                    break;
                case "--disclaimer":
                    Program.GetDisclaimer();
                    Mode = Enums.Modes.Quit;
                    break;
            }

            switch (lastPar)
            {
                case "--show-gimmick":
                case "--export-col-obj":
                    SecondaryFileName = arg;
                    break;
                case "--mlb-section":
                    MlbSect = arg;
                    break;
                case "--input":
                    FileName = arg;
                    break;
                case "--output":
                    Output = arg;
                    break;
                case "--magick-path":
                    MagickPath = arg;
                    break;
                case "--ffmpeg-path":
                    FFmpegPath = arg;
                    break;
                case "--msg-path":
                    StaticUtils.MsgFile = arg;
                    break;
                case "--no-envelopes":
                    StaticUtils.ExportEnvelopes = false;
                    break;
                case "--midi-file":
                    MidiFile = arg;
                    break;
                case "--bd-file":
                    BdFile = arg;
                    break;
                case "--scale-factor":
                    ScaleFactor = int.Parse(arg);
                    break;
                case "--reverb-strength":
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    StaticUtils.ReverbStrength = (short)(double.Parse(arg) * 10.0);
                    break;
                case "--replace-file":
                case "--replace-iso":
                    VFile = arg;
                    break;
                default:
                    break;
            }

            lastPar = arg;
        }

        if (args.Length <= 0 || !File.Exists(args[0])) return;
        if (Mode != Enums.Modes.NoAction) return;
        Mode = Enums.GuessAction(args[0]);
        if (Mode != Enums.Modes.ShowHelp)
        {
            FileName = args[0];
        }
    }

    /// <summary>
    /// Detect any obvious errors
    /// </summary>
    /// <returns>Exit code, if -1 then there were no errors found and execution can continue</returns>
    public int DetectAndDisplayErrors()
    {
        
        if (FileName == "" && Mode != Enums.Modes.ShowHelp)
        {
            StaticUtils.DecodeColors(
                "~-CError~--: Must specify input filename in this case! To see command line usage, append the ~-F--help~-- flag.");
            Console.WriteLine();
            return 1;
        }

        if (!File.Exists(FileName) && Mode != Enums.Modes.ShowHelp)
        {
            StaticUtils.DecodeColors("~-CError~--: Input file does not exist!");
            Console.WriteLine();
            return 2;
        }

        if (Mode == Enums.Modes.ShowHelp || !new FileInfo(FileName).IsReadOnly || Output == "") return -1;
        StaticUtils.DecodeColors("~-CError~--: Read-only file system");
        Console.WriteLine();
        return 3;

    }

    /// <summary>
    /// Show the app name and current file name
    /// </summary>
    public void ShowSignature()
    {
        if (!StaticUtils.SimpleOutput)
        {
            var suff = (StaticUtils.IsBeta ? " BETA" : "");
            StaticUtils.DecodeColors(
                $"~-BFlipnic File Tools {StaticUtils.DotFloatString(StaticUtils.LibVersion)}{suff}~--\n");
        }

        if (FileName == "" || StaticUtils.SimpleOutput) return;
        Console.WriteLine($"Filename: {FileName}");
        Console.Write("\n");
    }
}