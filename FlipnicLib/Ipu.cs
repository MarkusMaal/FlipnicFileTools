using FlipnicLib.Formats;

namespace FlipnicLib;

public abstract class Ipu : FormatBase
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
        var width = GetInt16(header, 0x8);
        var height = GetInt16(header, 0xA);
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
        var header = new byte[0x11];
        using var reader = new BinaryReader(stream);
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var readBytes = reader.Read(header, 0x0, header.Length);
        if (readBytes < 10) return "Not IPU file";
        
        var magic = GetString(header.Take(0x4).ToArray());
        var width = GetInt16(header, 0x8);
        var height = GetInt16(header, 0xA);
        var frames = GetInt32(header, 0xC);
        var endVideo = GetUInt32(header, 0x4);
        var flags = header[0x10];
        var mpegType = ((flags & 0x01) != 0) ? "MPEG-1" : "MPEG-2";
        var qScaleType = ((flags & 0x02) != 0 ) ? "Non-linear" : "Linear"; 
        var scanType = ((flags & 0x08) != 0 ) ? "Alternate" : "Zig Zag";
        var dctTypeDecode = ((flags & 0x80) != 0) ? "Decode" : "Not decoded";
        var intraDcPrecision = ((flags) & 0b11);
        var isProgressive = width < 512;
        var sInterlaced = isProgressive ? "Progressive" : "Interlaced";
        var bandWidthSum = 0L;
        var shiftRegister = new byte[4];
        var pos = 0x11;
        var cSum = 0;

        string[] colHeaders = ["Frame", "Offset", "Size"];
        List<string[]> rows = [];
        var offset = 0x11L;
        while (reader.BaseStream.Position < endVideo)
        {
            if (reader.BaseStream.Position % 0x8000 == 0)
            {
                var perc = Math.Round(reader.BaseStream.Position / (double)endVideo * 100.0, 2);
                StaticUtils.LiveLoadStatus = $"Parsing IPU ({DotFloatString((float)perc)}% complete)";
            }
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            _ = reader.Read(shiftRegister);
            if ((shiftRegister[0] + shiftRegister[1] == 0) && (shiftRegister[2] == 1) && (shiftRegister[3] == 0xB0))
            {
                bandWidthSum += cSum;
                rows.Add([(rows.Count + 1).ToString(), offset.ToString("X") + "h", StaticUtils.SimpleOutput ? cSum.ToString("X") + "h" : (GetFilesizeString(cSum) + " (" + cSum.ToString("X") + "h)")]);
                offset = reader.BaseStream.Position;
                cSum = 0;
                pos+=4;
                continue;
            }

            cSum += 1;
            pos++;
        }

        var table = StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
        var avgBitRate = GetFilesizeString((long)(bandWidthSum / (double)frames));
        string[] intraDcStrs = ["8-bits", "9-bits", "10-bits", "Invalid"];
        var pad = StaticUtils.SimpleOutput ? "" : " "; // lol
        return $"""
                IPU video stream
                
                Magic: {magic}
                Width: {width}
                Height: {height}
                Frames: {frames}
                
                Frame drawing: {sInterlaced} 
                
                Duration (NTSC): {GetMsAsDuration((long)(frames / (isProgressive ? 59.94 : 29.97) * 1000.0))}
                Duration {pad}(PAL): {GetMsAsDuration((long)(frames / (isProgressive ? 50.0 : 25.0) * 1000.0))}
                
                Video format: {mpegType}
                Q-Scale Type: {qScaleType} step
                Scan Type: {scanType}
                DCT Type Decoding: {dctTypeDecode}
                Intra DC Precision: {intraDcStrs[intraDcPrecision]}
                
                Avg. bitrate: {avgBitRate}/s
                
                Frame information:
                {table}
                """;
    }
}