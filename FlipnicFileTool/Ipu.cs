using System.Diagnostics;

namespace FlipnicFileTool;

public class Ipu
{
    public static void IpuConvert(string fileName, string outFile, string ffmpegPath)
    {
        var header = new byte[0x10];
        using var reader = new BinaryReader(File.Open(fileName, FileMode.Open));
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var read = reader.Read(header, 0, 0x10);
        if (read < 10)
        {
            throw new FileLoadException("Header read error:  " + fileName);
        }
        reader.Close();
        // try to guess video format
        var width = StaticUtils.GetInt16(header, 0x8);
        var height = StaticUtils.GetInt16(header, 0xA);
        var lowRes = width <= 256;
        var isPal = (!lowRes && height == 512) || Program.Pal;

        var frameRate = lowRes switch
        {
            false when !isPal => "29.97",
            true when isPal => "50",
            false when isPal => "25",
            _ => "59.94"
        };
        var ffmpegCommand = $"-r {frameRate} -i \"{fileName}\" -vf bwdif -c:v qtrle -pix_fmt rgb24 \"{outFile}\"";
        StaticUtils.ProcessFFmpeg(ffmpegPath, ffmpegCommand);
    }
}