using FlipnicLib;

namespace FlipnicFileTool.Help;

public abstract class HelpUtils
{
    public static List<HelpTopic>? Help;

    /// <summary>
    /// Generates the text that is displayed when the user passes a --help flag to args
    /// </summary>
    public static void GenerateHelp(bool quiet = false)

    {
        Help =
        [
            new HelpTopic("", "",
            [
                new HelpLine("input",
                    "File to open (for multiple inputs, specify args like this: --input FILE_A --input FILE_B)", [],
                    ""),
                new HelpLine("output", "File to write to", [], ""),
                new HelpLine("help", "Display help", [], ""),
                new HelpLine("disclaimer", "Display disclaimer", [], ""),
                new HelpLine("simple", "Use output that is easy to parse for computer programs", [], ""),
                new HelpLine("low-memory", "Reduces performance to save on memory usage", [], ""),
                new HelpLine("ffmpeg-path", "Path to FFmpeg (for audio/video conversion operations)", [], ""),
                new HelpLine("msg-path", "Path to JA.MSG file (optional)", [], "", false, "get-pseudo-code"),
                new HelpLine("version", "Displays the version number for FlipnicLib", [], ""),
            ])

        ];
        if (StaticUtils.IsBeta)
        {
            Help[0].AddLine(new HelpLine("test", "Change how the program behaves specifically for automated testing",
                [], "", true));
            Help[0].AddLine(new HelpLine("playground",
                "Runs code from the PlaygroundTools class with the configuration specified (development tool)", [],
                "", true));
        }

        Help.AddRange(
        [
            new HelpTopic(
                "VAB body files",
                "*.BD",
                [
                    new HelpLine("show-bd*", "List samples in the .BD file", ["input"], "*.BD", true),
                    new HelpLine("extract-samples", "Extract all samples from the .BD file (output = folder)",
                        ["input", "output"], "*.BD,*/", true),
                ]
            ),
            new HelpTopic(
                "Blob files",
                "*.BIN",
                [
                    new HelpLine("list-files*", "List all files inside this container file", ["input"], "*.BIN", true),
                    new HelpLine("extract-files", "Extract files inside the container to a folder (output = folder)",
                        ["input", "output"], "*.BIN,*/", true),
                    new HelpLine("extract-pak",
                        "Extract subfolders inside the container as PAK files (output = folder)", ["input", "output"],
                        "*.BIN,*/", true),
                    new HelpLine("replace-file [vfile]",
                        "Allows you to replace a single file inside a .BIN container (input = replacement file, output = BIN file, vfile = BIN file record name)",
                        ["input", "output"], "*,*.BIN,[*]", true),
                ]
            ),
            new HelpTopic(
                "Collision maps",
                "*.COL",
                [
                    new HelpLine("show-col*", "Display information about the collision map", ["input"], "*.COL", true),
                    new HelpLine("export-col-obj [mesh]",
                        "Create 3D-model from the COL file specified, input = COL file, output = OBJ file, mesh = specify either a specific section from COL file or ALL to export everything",
                        ["input", "output"], "*.COL,*.OBJ,[*]", true),
                ]
            ),
            new HelpTopic(
                "Dummy file",
                "DUMMY.DAT",
                [
                    new HelpLine("show-dummy*", "Display information about the dummy file", ["input"], "*.DAT", true),
                ]
            ),
            new HelpTopic
            (
                "Flipnic Camera sequences",
                "*.FPC",
                [
                    new HelpLine("show-fpc*", "Display data from .FPC file as human-readable text", ["input"], "*.FPC", true),
                    new HelpLine("convert-fpc-to-xml", "Convert .FPC file to .XML", ["input", "output"], "*.FPC,*.XML", true),
                    new HelpLine("convert-xml-to-fpc",
                        "Creates a .FPC file based on a .XML file compatible with this program", ["input", "output"],
                        "*.XML,*.FPC", true),
                    new HelpLine("generate-animation [frame count]",
                        "Create a camera animation by linearly interpolating between two .FPC files, 2 inputs, output and frame count",
                        ["input", "output"], "*.FPC,*.FPC,[uint32]", true)
                ]
            ),
            new HelpTopic(
                "Path Sequences",
                "*.FPD",
                [
                    new HelpLine("show-fpd*", "Display general information about the file", ["input"], "*.FPD", true),
                    new HelpLine("export-fpd-obj",
                        "Export .FPD file as a 3D model (Wavefront OBJ), where the trajectory is drawn as a line",
                        ["input", "output"], "*.FPD,*.OBJ", true),
                ]
            ),
            new HelpTopic(
                "Texture list",
                "*.FTL",
                [
                    new HelpLine("show-ftl*", "Display textures list stored inside the file as a table", ["input"],
                        "*.FTL", true)
                ]
            ),
            new HelpTopic(
                "VAB header files",
                "*.HD",
                [
                    new HelpLine("show-hd*", "List programs in the .HD file", ["input"], "*.HD", true),
                    new HelpLine("convert-sf2", "Allows you to convert soundbank to .SF2 (specify .HD file as input)",
                        ["input", "output"], "*.HD,*.SF2", true),
                    new HelpLine("convert-sfx-sf2", "Allows you to convert sound effect voicebank to .SF2 (specify .HD file, which doesn't have a corresponding .MID file, as input)",
                        ["input", "output"], "*.HD,*.SF2", true),
                    new HelpLine("no-envelopes", "Doesn't export envelopes (attack, decay, sustain, release)", [], "",
                        false, "convert-sf2"),
                    new HelpLine("synthesize-wav",
                        "Creates a .WAV file in addition to the .SF2 file (use with --convert-sf2 option)", [], "",
                        false, "convert-sf2"),
                    new HelpLine("fake-sustain-rate",
                        "Simulate sustain rate by tweaking output decay rate/sustain level (use with --convert-sf2 option)",
                        [], "",  false, "convert-sf2"),
                    new HelpLine("reverb-strength [value]",
                        $"Adjust reverb strength as a percentage (default: {StaticUtils.DotFloatString((float)Math.Round(StaticUtils.ReverbStrength / 10.0, 1))}%)",
                        [], "[float]", false,"convert-sf2"),
                    new HelpLine("midi-file [path]",
                        "Manually specify a .MID file to use for conversion (default is input file path, but with .MID extension)",
                        [], "[*.MID]", false,"convert-sf2"),
                    new HelpLine("bd-file [path]",
                        "Manually specify a .BD file to use for conversion (default is input file path, but with .BD extension)",
                        [], "[*.BD]", false,"convert-sf2"),
                ]
            ),
            new HelpTopic(
                "Save file icon",
                "*.ICO",
                [
                    new HelpLine("show-ico*", "Display information about the save icon", ["input"], "*.ICO", true),
                    new HelpLine("convert-ico-texture", "Converts save icon texture to PNG", ["input", "output"],
                        "*.ICO,*.PNG", true),
                    new HelpLine("convert-ico-obj", "Converts save icon to Wavefront OBJ model", ["input", "output"],
                        "*.ICO,*.OBJ", true),
                ]
            ),
            new HelpTopic(
                "Video files",
                "*.IPU",
                [
                    new HelpLine("show-ipu*", "Display basic information about the IPU file", ["input"], "*.IPU", true),
                    new HelpLine("convert-ipu", "Uses FFmpeg to convert .IPU file to .M2V", ["input", "output"],
                        "*.IPU,*.M2V", true),
                    new HelpLine("ipu-duct-tape",
                        "Attempts to fix a mangled .IPU file (use --pal and/or --progressive when applicable)",
                        ["input"], "*.IPU", true)
                ]
            ),
            new HelpTopic(
                "PlayStation 2 ISO file",
                "*.ISO",
                [
                    new HelpLine("show-iso*", "Display a list of files stored inside the ISO file", ["input"], "*.ISO", true),
                    new HelpLine("extract-iso", "Extract all files from the .ISO file (output = folder)",
                        ["input", "output"], "*.ISO,*/", true),
                    new HelpLine("replace-iso [vfile]",
                        "Replace an existing file inside the .ISO file with new contents (input = replacement file, output = ISO file, vfile = ISO file record name)",
                        ["input", "output"], "*,*.ISO,[*]", true),
                ]
            ),
            new HelpTopic(
                "Layout files",
                "*.LAY",
                [
                    new HelpLine("show-lay*", "List layout data in human-readable format", ["input"], "*.LAY", true)
                ]
            ),
            new HelpTopic(
                "Environment lighting",
                "*.LIT",
                [
                    new HelpLine("show-lit*", "Display color intensity values", ["input"], "*.LIT", true)
                ]
            ),
            new HelpTopic(
                "Resource files",
                "*.LP4",
                [
                    new HelpLine("show-lp4*", "Display general information about the file", ["input"], "*.LP4", true),
                    new HelpLine("export-lp4-json", "Convert LP4 to JSON", ["input", "output"], "*.LP4,*.JSON", true),
                    new HelpLine("export-obj", "Export models from the LP4 file as Wavefront OBJ", ["input", "output"],
                        "*.LP4,*.OBJ", true),
                    new HelpLine("export-box-obj", "Export bounding box from the LP4 file as Wavefront OBJ",
                        ["input", "output"], "*.LP4,*.OBJ", true),
                    new HelpLine("alternate-normals",
                        "Use a different method for decoding normal vectors (required for some files)", [], "",
                        false, "export-lp4-json,export-obj"),
                ]
            ),
            new HelpTopic(
                "MIDI sequences",
                "*.MID",
                [
                    new HelpLine("show-midi*", "List MIDI events", ["input"], "*.MID", true)
                ]
            ),
            new HelpTopic(
                "Menu files",
                "*.MLB",
                [
                    new HelpLine("show-mlb*", "Display all menu elements as a table", ["input"], "*.MLB", true),
                    new HelpLine("generate-mockup",
                        "Combine texture files to create a mockup for the menu file (requires ImageMagick v7 or later)",
                        ["input", "output"], "*.MLB,*.PNG", true),
                    new HelpLine("pal", "Use 512 lines instead of 480 for generated images", [], "",
                        false, "show-mlb,ipu-duct-tape,convert-pss-mp4,convert-ipu,generate-mockup,generate-pss"),
                    new HelpLine("mlb-section [name]", "Combine only a specific section of the menu", [], "[*]",
                        false, "generate-mockup"),
                ]
            ),
            new HelpTopic(
                "Message file",
                "JA.MSG",
                [
                    new HelpLine("show-messages*", "Display all strings stored in the file", ["input"], "*.MSG", true),
                    new HelpLine("generate-msg",
                        "Generates a message file from a text file containing strings separated by new lines (input = txt file, output = msg file)",
                        ["input", "output"], "*.TXT,*.MSG", true),
                ]
            ),
            new HelpTopic(
                "Subfolders",
                "*.PAK",
                [
                    new HelpLine("list-pak*", "List all files inside the subdirectory", ["input"], "*.PAK", true),
                    new HelpLine("replace-pak [vfile]",
                        "Replace a file inside the subdirectory (output = pak, input = replacement, vfile = name inside PAK)",
                        ["input", "output"], "*,*.PAK,[*]", true),
                ]
            ),
            new HelpTopic(
                "Interleaved audio/video stream",
                "*.PSS",
                [
                    new HelpLine("list-pss-streams*", "List all available streams in a .PSS file", ["input"], "*.PSS", true),
                    new HelpLine("extract-pss-streams", "Demux a .PSS file to .IPU and .INT files (output = folder)",
                        ["input", "output"], "*.PSS,*/", true),
                    new HelpLine("convert-int", "Convert .INT file to .WAV", ["input", "output"], "*.INT,*.WAV", true),
                    new HelpLine("convert-pss-mp4", "Convert .PSS file directly to .MP4 file with audio streams",
                        ["input", "output"], "*.PSS,*.MP4", true),
                    new HelpLine("pal", "Force 25/50 frames per second when converting video files", [], "",
                        false, "show-mlb,ipu-duct-tape,convert-pss-mp4,convert-ipu,generate-mockup,generate-pss"),
                    new HelpLine("crop-alpha", "Crops out the alpha mask from low-res FMVs", [], "", false, "convert-pss-mp4"),
                    new HelpLine("crop-rgb", "Crops out the RGB part of low-res FMVs", [], "", false, "convert-pss-mp4"),
                    new HelpLine("scale-factor [n]",
                        "Scales up the final video *n (NOTE: you can't apply crop and scale factor at the same time)",
                        [], "[uint32]", false,"convert-pss-mp4"),
                    new HelpLine("generate-pss [int]",
                        "Allows you to generate an interleaved .PSS file, append --pal flag if you want to generate PAL streams (int = INT file, input = IPU file, output = PSS file)",
                        ["input", "output"], "*.IPU,*.PSS,[*.INT]", true),
                    new HelpLine("progressive",
                        "Uses settings required for progressive scan FMVs (useful only with --generate-pss)", [], "",
                        false, "convert-pss-mp4,ipu-duct-tape,convert-ipu,generate-pss")
                ]
            ),
            new HelpTopic(
                "Source code control files",
                "*.SCC",
                [
                    new HelpLine("show-vss*", "Displays information stored inside the VSSVER.SCC file", ["input"],
                        "*.SCC", true)
                ]
            ),
            new HelpTopic(
                "Game Executable",
                "*.*",
                [
                    new HelpLine("show-elf*", "Display some information about the game executable", ["input"], "*", true),
                ]
            ),
            new HelpTopic(
                "Stage information files",
                "*.SST",
                [
                    new HelpLine("show-sst-resources", "Display all resources referenced by SST file", ["input"],
                        "*.SST", true),
                    new HelpLine("show-sst-toc*", "Display table of contents of the SST file", ["input"], "*.SST", true),
                    new HelpLine("show-sst-missions", "Allows you to display missions stored inside a FNECMN.SST file",
                        ["input"], "*.SST", true),
                    new HelpLine("show-sst-respawns", "Allows you to display respawn points stored inside a stage file",
                        ["input"], "*.SST", true),
                    new HelpLine("show-cameras", "Display camera metadata stored inside the SST file", ["input"],
                        "*.SST", true),
                    new HelpLine("show-draw-distance", "Allows you to display the draw distance and mirror attribute for the stage",
                        ["input"], "*.SST", true),
                    new HelpLine("show-gimmick [name]", "Display a gimmick (name from TOC)", ["input"], "*.SST,[*]", true),
                    new HelpLine("get-pseudo-code",
                        "Transform stage event into something that's somewhat human-readable", ["input"], "*.SST", true),
                    new HelpLine("change-count [name],[count]",
                        "Allows you to resize a specific section of the .SST file", ["input"], "*.SST,[*],[uint32]", true)
                ]
            ),
            new HelpTopic(
                "Sound files",
                "*.SVAG",
                [
                    new HelpLine("convert-svag", "Converts a .SVAG file to .WAV", ["input", "output"], "*.SVAG,*.WAV", true)
                ]
            ),
            new HelpTopic(
                "Texture files",
                "*.TM2",
                [
                    new HelpLine("show-tim2*", "Display information about a texture file", ["input"], "*.TM2", true),
                    new HelpLine("convert-tim2", "Converts a texture file to a bitmap (.PNG file)", ["input", "output"],
                        "*.TM2,*.PNG", true),
                ]
            ),
            new HelpTopic(
                "Vibration data",
                "*.VSD",
                [
                    new HelpLine("show-vsd*", "Display vibration strength values", ["input"], "*.VSD", true)
                ]
            ),
        ]);

        if (quiet) return;
        foreach (var ht in Help)
        {
            ht.DisplayTopic();
            Console.WriteLine();
        }
    }
}