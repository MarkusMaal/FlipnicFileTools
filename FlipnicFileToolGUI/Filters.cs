using Avalonia.Platform.Storage;

namespace FlipnicFileToolGUI;

public abstract class Filters
{
    public static FilePickerFileType AllSupported { get; } = new("All supported file formats")
    {
        Patterns = ["*.BIN", "*.FPC", "*.FPD", "*.SST", "*.PSS", "*.MSG", "*.LP4", "*.MLB", "*.TM2", "*.MID", "*.HD", "*.BD", "*.SVAG", "*.INT", "*.VAG", "*.VSD", "*.CSV", "*.TXT", "*.XML", "*.ICO", "*.IPU", "*.LIT", "*.LAY", "*.COL", "*.ISO", "*.iso", "VSSVER.SCC", "*.FTL", "SLUS_291.49", "SLES_520.65", "SCPS_150.50", "SLUS_211.57", "SYSTEM.CNF", "DUMMY.DAT"]
    };
    
    public static FilePickerFileType BinFile { get; } = new("BIN files (.BIN)")
    {
        Patterns = ["*.BIN"]
    };
    
    public static FilePickerFileType SccFile { get; } = new("Source code control files (VSSVER.SCC)")
    {
        Patterns = ["VSSVER.SCC"]
    };

    
    public static FilePickerFileType DummyFile { get; } = new("Dummy file (DUMMY.DAT)")
    {
        Patterns = ["DUMMY.DAT"]
    };

    public static FilePickerFileType FtlFile { get; } = new("Texture list (*.FTL)")
    {
        Patterns = ["*.FTL"]
    };
    public static FilePickerFileType FpdFile { get; } = new("Fixed Path Data (.FPD)")
    {
        Patterns = ["*.FPD"]
    };
    
    public static FilePickerFileType ColFile { get; } = new("Collision maps (.COL)")
    {
        Patterns = ["*.COL"]
    };
    
    public static FilePickerFileType LitFile { get; } = new("Light maps (.LIT)")
    {
        Patterns = ["*.LIT"]
    };
    public static FilePickerFileType FpnFpc { get; } = new("Camera sequences (.FPC)")
    {
        Patterns = ["*.FPC"]
    };
    public static FilePickerFileType FpnSst { get; } = new("Stage files (.SST)")
    {
        Patterns = ["*.SST"]
    };
    public static FilePickerFileType SonyPss { get; } = new("Interleaved video streams (.PSS)")
    {
        Patterns = ["*.PSS"]
    };
    public static FilePickerFileType FpnMsg { get; } = new("Flipnic message files (.MSG)")
    {
        Patterns = ["*.MSG"]
    };
    public static FilePickerFileType FpnLp4 { get; } = new("Flipnic resource file (.LP4)")
    {
        Patterns = ["*.LP4"]
    };
    public static FilePickerFileType FpnMlb { get; } = new("Menu file (.MLB)")
    {
        Patterns = ["*.MLB"]
    };
    public static FilePickerFileType LayFile { get; } = new("Stage layout file (.LAY)")
    {
        Patterns = ["*.LAY"]
    };
    
    public static FilePickerFileType SonyTim2 { get; } = new("PlayStation texture files (.TM2)")
    {
        Patterns = ["*.TM2"]
    };
    public static FilePickerFileType MidiFile { get; } = new("General MIDI (.MID)")
    {
        Patterns = ["*.MID"]
    };
    public static FilePickerFileType HdFile { get; } = new("VAB soundbank headers (.HD)")
    {
        Patterns = ["*.HD"]
    };
    public static FilePickerFileType BdFile { get; } = new("VAB soundbank body (.BD)")
    {
        Patterns = ["*.BD"]
    };
    public static FilePickerFileType VsdFile { get; } = new("Vibration strength data (.VSD)")
    {
        Patterns = ["*.VSD"]
    };
    public static FilePickerFileType SvagFile { get; } = new("Sony Compressed ADPCM audio (.SVAG/.INT)")
    {
        Patterns = ["*.SVAG", "*.VAG", "*.INT"]
    };
    
    public static FilePickerFileType IpuFile { get; } = new("IPU video stream (.IPU)")
    {
        Patterns = ["*.IPU"]
    };

    
    public static FilePickerFileType SaveIcon { get; } = new("PlayStation 2 save icon (.ICO)")
    {
        Patterns = ["*.ICO"]
    };
    public static FilePickerFileType IsoFile { get; } = new("Disc image (.ISO)")
    {
        Patterns = ["*.ISO", "*.iso"]
    };
    public static FilePickerFileType GameElf { get; } = new("Game Executable (SLUS_291.49/SLES_520.65/SCPS_150.50/SLUS_211.57)")
    {
        Patterns = ["SLUS_291.49", "SLES_520.65", "SCPS_150.50", "SLUS_211.57"]
    };
    
    public static FilePickerFileType SysCnf { get; } = new("PSX/PS2 game information (SYSTEM.CNF)")
    {
        Patterns = ["SYSTEM.CNF"]
    };

    public static FilePickerFileType TxtFile { get; } = new("Text files (.TXT)")
    {
        Patterns = ["*.TXT"],
        MimeTypes = ["text/plain"],
        AppleUniformTypeIdentifiers = ["public.text"]
    };



    public static FilePickerFileType XmlFile { get; } = new("Extensible Markup Language files (.XML)")
    {
        Patterns = ["*.XML"],
        MimeTypes = ["text/xml", "application/xml"],
        AppleUniformTypeIdentifiers = ["public.xml"]
    };

    public static FilePickerFileType CsvFile { get; } = new("Comma Separated Values (.CSV)")
    {
        Patterns = ["*.CSV"],
        MimeTypes = ["text/csv"],
        AppleUniformTypeIdentifiers = ["public.comma-separated-values-text"]
    };

    public static FilePickerFileType PngFile { get; } = new("Portable Network Graphics (.PNG)")
    {
        Patterns = ["*.PNG"],
        MimeTypes = ["image/png"],
        AppleUniformTypeIdentifiers = ["public.png"]
    };

    public static FilePickerFileType WavFile { get; } = new("Wave file (.WAV)")
    {
        Patterns = ["*.WAV"],
        MimeTypes = ["audio/wav"],
        AppleUniformTypeIdentifiers = ["com.microsoft.waveform-audio"]
    };

    public static FilePickerFileType ObjFile { get; } = new("Wavefront OBJ (.OBJ)")
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