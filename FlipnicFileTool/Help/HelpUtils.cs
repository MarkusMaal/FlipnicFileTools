namespace FlipnicFileTool.Help;

public abstract class HelpUtils
{
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
                new HelpLine("simple", "Use output that is easy to parse for computer programs"),
                new HelpLine("low-memory", "Reduces performance to save on memory usage"),
                new HelpLine("magick-path", "Path to ImageMagick executable (may not be needed dep. on what you're trying to do)"),
                new HelpLine("ffmpeg-path", "Path to FFmpeg (for audio/video conversion operations)"),
                new HelpLine("msg-path", "Path to JA.MSG file (optional)"),
                new HelpLine("png", "Use PNG instead of BMP (for transparency and smaller file sizes)"), 
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
                "Stage information files",
                "*.SST",
                [
                    new HelpLine("show-sst-resources", "Display all resources referenced by SST file"),
                    new HelpLine("show-sst-toc*", "Display table of contents of the SST file"),
                    new HelpLine("show-gimmick [name]", "Display a gimmick (name from TOC)"),
                    new HelpLine("get-pseudo-code", "Transform stage event into something that's somewhat human-readable"),
                ]
            ),
            new(
                "Message file",
                "JA.MSG",
                [
                    new HelpLine("show-messages*", "Display all strings stored in the file")
                ]
            ),
            new(
                "Interleaved audio/video stream",
                "*.PSS",
                [
                    new HelpLine("list-pss-streams*", "List all available streams in a .PSS file"),
                    new HelpLine("extract-pss-streams", "Demux a .PSS file to .IPU and .INT files (output = folder)"),
                    new HelpLine("convert-int", "Convert .INT file to .WAV"),
                    new HelpLine("convert-pss-mov", "Convert .PSS file directly to .MOV file with audio streams"),
                    new HelpLine("pal", "Force 25/50 frames per second when converting video files"),
                ]
            ),
            new (
                "Video files",
                "*.IPU",
                [
                    new HelpLine("show-ipu", "Display basic information about the IPU file"),
                    new HelpLine("convert-ipu", "Uses FFmpeg to convert .IPU file to .MOV")
                ]
            ),
            new(
                "Blob files",
                "*.BIN",
                [
                    new HelpLine("list-files*", "List all files inside this container file"),
                    new HelpLine("extract-files", "Extract files inside the container to a folder (output = folder)"),
                ]
            ),
            new(
                "Resource files",
                "*.LP4",
                [
                    new HelpLine("show-lp4*", "Display general information about the file"),
                    new HelpLine("export-obj", "Export models from the LP4 file as Wavefront OBJ")
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
                    new HelpLine("convert-tim2", "Converts a texture file to a bitmap (.BMP file)"),
                    new HelpLine("grayscale", "Set palette to grayscale (black and white)"),
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
                    new HelpLine("no-velocity", "Doesn't export volume levels (may fix some playback issues)"),
                    new HelpLine("midi-file [path]", "Manually specify a .MID file to use for conversion (default is input file path, but with .MID extension)"),
                    new HelpLine("bd-file [path]", "Manually specify a .BD file to use for conversion (default is input file path, but with .BD extension)"),
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
                "Save file icon",
                "*.ICO",
                [
                    new HelpLine("show-ico*", "Display information about the save icon"),
                    new HelpLine("convert-ico-texture", "Converts save icon texture to PNG"),
                    new HelpLine("convert-ico-obj", "Converts save icon to Wavefront OBJ model"),
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