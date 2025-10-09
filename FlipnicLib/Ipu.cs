namespace FlipnicLib;

public abstract class Ipu
{
    
    /// <summary>
    /// Convert IPU file to M2V
    /// </summary>
    /// <param name="fileName">Full path to the input .IPU file</param>
    /// <param name="outFile">Full path to the output .M2V file</param>
    /// <param name="ffmpegPath">Full path to the FFmpeg executable</param>
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
        var isPal = (!lowRes && height == 512) || StaticUtils.Pal;

        var frameRate = lowRes switch
        {
            false when !isPal => "29.97",
            true when isPal => "50",
            false when isPal => "25",
            _ => "59.94"
        };
        var ffmpegCommand = $"-y -r {frameRate} -i \"{fileName}\" -vf bwdif -c:v mpeg2video -q:v 1 \"{outFile}\"";
        StaticUtils.ProcessFFmpeg(ffmpegPath, ffmpegCommand);
    }

    /// <summary>
    /// Get information about the IPU file, including resolution, magic and frame count
    /// </summary>
    /// <param name="stream">Input .IPU file stream</param>
    /// <returns>A string to display to the user containing the info</returns>
    public static string GetInfoAsString(Stream stream)
    {
        var header = new byte[0x10];
        using var reader = new BinaryReader(stream);
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var readBytes = reader.Read(header, 0x0, 0x10);
        if (readBytes < 10) return "Not IPU file";
        
        var magic = StaticUtils.GetString(header.Take(0x4).ToArray());
        var width = StaticUtils.GetInt16(header, 0x8);
        var height = StaticUtils.GetInt16(header, 0xA);
        var frames = StaticUtils.GetInt32(header, 0xC);
        return $"""
                IPU video stream
                
                Magic: {magic}
                Width: {width}
                Height: {height}
                Frames: {frames}
                """;
    }
}