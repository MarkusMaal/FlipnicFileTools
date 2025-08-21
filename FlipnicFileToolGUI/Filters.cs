using Avalonia.Platform.Storage;

namespace FlipnicFileToolGUI;

public class Filters
{
    public static FilePickerFileType BinFile { get; } = new("BIN files")
    {
        Patterns = ["*.BIN"]
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
}