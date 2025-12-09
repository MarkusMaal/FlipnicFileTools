using Avalonia.Platform.Storage;

namespace FlipnicFileToolGUI;

public abstract class Filters
{
    public static FilePickerFileType AllSupported { get; } = new("All supported file formats")
    {
        Patterns = ["*.BIN", "*.FPC", "*.FPD", "*.SST", "*.PSS", "*.MSG", "*.LP4", "*.MLB", "*.TM2", "*.MID", "*.HD", "*.BD", "*.SVAG", "*.INT", "*.VAG", "*.VSD", "*.CSV", "*.TXT", "*.XML", "*.ICO", "*.LIT", "*.LAY", "*.COL"]
    };
    
    public static FilePickerFileType BinFile { get; } = new("BIN files")
    {
        Patterns = ["*.BIN"]
    };
    
    public static FilePickerFileType FpdFile { get; } = new("Fixed Path Data")
    {
        Patterns = ["*.FPD"]
    };
    
    public static FilePickerFileType ColFile { get; } = new("Collision maps")
    {
        Patterns = ["*.COL"]
    };
    
    public static FilePickerFileType LitFile { get; } = new("Light maps")
    {
        Patterns = ["*.LIT"]
    };
    public static FilePickerFileType FpnFpc { get; } = new("Camera sequences")
    {
        Patterns = ["*.FPC"]
    };
    public static FilePickerFileType FpnSst { get; } = new("Stage files")
    {
        Patterns = ["*.SST"]
    };
    public static FilePickerFileType SonyPss { get; } = new("Interleaved video streams")
    {
        Patterns = ["*.PSS"]
    };
    public static FilePickerFileType FpnMsg { get; } = new("Flipnic message files")
    {
        Patterns = ["*.MSG"]
    };
    public static FilePickerFileType FpnLp4 { get; } = new("Flipnic resource file")
    {
        Patterns = ["*.LP4"]
    };
    public static FilePickerFileType FpnMlb { get; } = new("Menu file")
    {
        Patterns = ["*.MLB"]
    };
    
    public static FilePickerFileType SonyTim2 { get; } = new("PlayStation texture files")
    {
        Patterns = ["*.TM2"]
    };
    public static FilePickerFileType MidiFile { get; } = new("General MIDI")
    {
        Patterns = ["*.MID"]
    };
    public static FilePickerFileType HdFile { get; } = new("VAB soundbank headers")
    {
        Patterns = ["*.HD"]
    };
    public static FilePickerFileType BdFile { get; } = new("VAB soundbank body")
    {
        Patterns = ["*.BD"]
    };
    public static FilePickerFileType VsdFile { get; } = new("Vibration strength data")
    {
        Patterns = ["*.VSD"]
    };
    public static FilePickerFileType SvagFile { get; } = new("Sony Compressed ADPCM audio")
    {
        Patterns = ["*.SVAG", "*.VAG", "*.INT"]
    };
    
    public static FilePickerFileType IpuFile { get; } = new("IPU video stream")
    {
        Patterns = ["*.IPU"]
    };

    
    public static FilePickerFileType SaveIcon { get; } = new("PlayStation 2 save icon")
    {
        Patterns = ["*.ICO"]
    };

    public static FilePickerFileType TxtFile { get; } = new("Text files")
    {
        Patterns = ["*.TXT"],
        MimeTypes = ["text/plain"],
        AppleUniformTypeIdentifiers = ["public.text"]
    };



    public static FilePickerFileType XmlFile { get; } = new("Extensible Markup Language files")
    {
        Patterns = ["*.XML"],
        MimeTypes = ["text/xml", "application/xml"],
        AppleUniformTypeIdentifiers = ["public.xml"]
    };

    public static FilePickerFileType CsvFile { get; } = new("Comma Separated Values")
    {
        Patterns = ["*.CSV"],
        MimeTypes = ["text/csv"],
        AppleUniformTypeIdentifiers = ["public.comma-separated-values-text"]
    };

    public static FilePickerFileType PngFile { get; } = new("Portable Network Graphics")
    {
        Patterns = ["*.PNG"],
        MimeTypes = ["image/png"],
        AppleUniformTypeIdentifiers = ["public.png"]
    };

    public static FilePickerFileType WavFile { get; } = new("Wave file")
    {
        Patterns = ["*.WAV"],
        MimeTypes = ["audio/wav"],
        AppleUniformTypeIdentifiers = ["com.microsoft.waveform-audio"]
    };

    public static FilePickerFileType ObjFile { get; } = new("Wavefront OBJ")
    {
        Patterns = ["*.OBJ"],
        MimeTypes = ["model/obj", "application/prs.wavefront-obj", "application/x-tgif"],
        AppleUniformTypeIdentifiers = ["public.text"]
    };

    public static FilePickerFileType Executable { get; } = new("Executables")
    {
        MimeTypes =
        [
            "application/x-mach-binary", "application/vnd.microsoft.portable-executable", "application/x-pie-executable"
        ],
        AppleUniformTypeIdentifiers = ["public.unix-executable"]
    };
}