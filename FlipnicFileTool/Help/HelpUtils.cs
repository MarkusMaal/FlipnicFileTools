using FlipnicLib;

namespace FlipnicFileTool.Help;

public abstract class HelpUtils
{
    /// <summary>
    /// Generates the text that is displayed when the user passes a --help flag to args
    /// </summary>
    public static void GenerateHelp()
    {
        HelpTopic[] help =
        [
            new
            (
            "",
            "", [
                new HelpLine("input", "File to open"),
                new HelpLine("output", "File to write to"),
                new HelpLine("help", "Display help"),
                new HelpLine("disclaimer", "Display disclaimer"),
                new HelpLine("simple", "Use output that is easy to parse for computer programs"),
                new HelpLine("low-memory", "Reduces performance to save on memory usage"),
                new HelpLine("magick-path", "Path to ImageMagick executable (may not be needed dep. on what you're trying to do)"),
                new HelpLine("ffmpeg-path", "Path to FFmpeg (for audio/video conversion operations)"),
                new HelpLine("msg-path", "Path to JA.MSG file (optional)"),
                new HelpLine("test", "Change how the program behaves specifically for automated testing"), 
                new HelpLine("version", "Displays the version number for FlipnicLib"),]
            ),
            new
            (
                "Flipnic Camera sequences",
                "*.FPC",
                [
                    new HelpLine("show-fpc*", "Display data from .FPC file as human-readable text"),
                    new HelpLine("convert-fpc-to-xml", "Convert .FPC file to .XML"),
                ]
            ),
            new(
                "Path Sequences",
                "*.FPD",
                [
                    new HelpLine("show-fpd*", "Display general information about the file"),
                    new HelpLine("export-fpd-obj", "Export .FPD file as a 3D model (Wavefront OBJ), where the trajectory is drawn as a line"),
                ]
            ),
            new(
                "Stage information files",
                "*.SST",
                [
                    new HelpLine("show-sst-resources", "Display all resources referenced by SST file"),
                    new HelpLine("show-sst-toc*", "Display table of contents of the SST file"),
                    new HelpLine("show-cameras", "Display camera metadata stored inside the SST file"),
                    new HelpLine("show-gimmick [name]", "Display a gimmick (name from TOC)"),
                    new HelpLine("get-pseudo-code", "Transform stage event into something that's somewhat human-readable"),
                ]
            ),
            new(
                "Message file",
                "JA.MSG",
                [
                    new HelpLine("show-messages*", "Display all strings stored in the file"),
                    new HelpLine("generate-msg", "Generates a message file from a text file containing strings separated by new lines (input = txt file, output = msg file)"),
                ]
            ),
            new(
                "Interleaved audio/video stream",
                "*.PSS",
                [
                    new HelpLine("list-pss-streams*", "List all available streams in a .PSS file"),
                    new HelpLine("extract-pss-streams", "Demux a .PSS file to .IPU and .INT files (output = folder)"),
                    new HelpLine("convert-int", "Convert .INT file to .WAV"),
                    new HelpLine("convert-pss-mp4", "Convert .PSS file directly to .MP4 file with audio streams"),
                    new HelpLine("pal", "Force 25/50 frames per second when converting video files"),
                    new HelpLine("crop-alpha", "Crops out the alpha mask from low-res FMVs"),
                    new HelpLine("crop-rgb", "Crops out the RGB part of low-res FMVs"),
                    new HelpLine("scale-factor [n]", "Scales up the final video *n (NOTE: you can't apply crop and scale factor at the same time)")
                ]
            ),
            new (
                "Video files",
                "*.IPU",
                [
                    new HelpLine("show-ipu*", "Display basic information about the IPU file"),
                    new HelpLine("convert-ipu", "Uses FFmpeg to convert .IPU file to .M2V")
                ]
            ),
            new (
                "Source code control files",
                "*.SCC",
                [
                    new HelpLine("show-vss*", "Displays information stored inside the VSSVER.SCC file")
                ]
            ),
            new(
                "Resource files",
                "*.LP4",
                [
                    new HelpLine("show-lp4*", "Display general information about the file"),
                    new HelpLine("export-obj", "Export models from the LP4 file as Wavefront OBJ"),
                    new HelpLine("export-box-obj", "Export bounding box from the LP4 file as Wavefront OBJ"),
                    new HelpLine("alternate-normals", "Use a different method for decoding normal vectors (required for some files)")
                ]
            ),
            new(
                "Collision maps",
                "*.COL",
                [
                    new HelpLine("show-col*", "Display information about the collision map"),
                    new HelpLine("export-col-obj [mesh]", "Create 3D-model from the COL file specified, input = COL file, output = OBJ file, mesh = specify either a specific section from COL file or ALL to export everything"),
                ]
            ),
            new(
                "Menu files",
                "*.MLB",
                [
                    new HelpLine("show-mlb*", "Display all menu elements as a table"),
                    new HelpLine("generate-mockup", "Combine texture files to create a mockup for the menu file (requires ImageMagick v7 or later)"),
                    new HelpLine("pal", "Use 512 lines instead of 480 for generated images"),
                    new HelpLine("mlb-section [name]", "Combine only a specific section of the menu"),
                ]
            ),
            new(
                "Texture files",
                "*.TM2",
                [
                    new HelpLine("show-tim2*", "Display information about a texture file"),
                    new HelpLine("convert-tim2", "Converts a texture file to a bitmap (.PNG file)"),
                ]
            ),
            new(
                "Sound files",
                "*.SVAG",
                [
                    new HelpLine("convert-svag", "Converts a .SVAG file to .WAV")
                ]
            ),
            new(
                "VAB header files",
                "*.HD",
                [
                    new HelpLine("show-hd*", "List programs in the .HD file"),
                    new HelpLine("convert-sf2", "Allows you to convert soundbank to .SF2 (specify .HD file as input)"),
                    new HelpLine("no-envelopes", "Doesn't export envelopes (attack, decay, sustain, release)"),
                    new HelpLine("reverb-strength [value]", $"Adjust reverb strength as a percentage (default: {StaticUtils.DotFloatString((float)Math.Round(StaticUtils.ReverbStrength / 10.0, 1))}%)"),
                    new HelpLine("midi-file [path]", "Manually specify a .MID file to use for conversion (default is input file path, but with .MID extension)"),
                    new HelpLine("bd-file [path]", "Manually specify a .BD file to use for conversion (default is input file path, but with .BD extension)"),
                ]
            ),
            new(
                "VAB body files",
                "*.BD",
                [
                    new HelpLine("show-bd*", "List samples in the .BD file"),
                    new HelpLine("extract-samples", "Extract all samples from the .BD file (output = folder)"),
                ]
            ),
            new(
                "MIDI sequences",
                "*.MID",
                [
                    new HelpLine("show-midi*", "List MIDI events")
                ]
            ),
            new(
                "Vibration data",
                "*.VSD",
                [
                    new HelpLine("show-vsd*", "Display vibration strength values")
                ]
            ),
            new(
                "Layout files",
                "*.LAY",
                [
                    new HelpLine("show-lay*", "List layout data in human-readable format")
                ]
            ),
            new(
                "Environment lighting",
                "*.LIT",
                [
                    new HelpLine("show-lit*", "Display color intensity values")
                ]
            ),
            new(
                "Texture list",
                "*.FTL",
                [
                    new HelpLine("show-ftl*", "Display textures list stored inside the file as a table")
                ]
            ),
            new(
                "Save file icon",
                "*.ICO",
                [
                    new HelpLine("show-ico*", "Display information about the save icon"),
                    new HelpLine("convert-ico-texture", "Converts save icon texture to PNG"),
                    new HelpLine("convert-ico-obj", "Converts save icon to Wavefront OBJ model"),
                ]
            ),
            new(
                "Blob files",
                "*.BIN",
                [
                    new HelpLine("list-files*", "List all files inside this container file"),
                    new HelpLine("extract-files", "Extract files inside the container to a folder (output = folder)"),
                    new HelpLine("replace-file [vfile]", "Allows you to replace a single file inside a .BIN container (input = replacement file, output = BIN file, vfile = BIN file record name)"),
                ]
            ),
            new(
                "PlayStation 2 ISO file",
                "*.ISO",
                [
                    new HelpLine("show-iso*", "Display a list of files stored inside the ISO file"),
                    new HelpLine("extract-iso", "Extract all files from the .ISO file (output = folder)"),
                    new HelpLine("replace-iso [vfile]", "Replace an existing file inside the .ISO file with new contents (input = replacement file, output = ISO file, vfile = ISO file record name)"),
                ]
            ),
        ];

        foreach (var ht in help)
        {
            ht.DisplayTopic();
            Console.WriteLine();
        }
    }
}