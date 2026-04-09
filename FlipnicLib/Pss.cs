using FlipnicLib.Formats;

namespace FlipnicLib;

public class Pss(string fileName) : FormatBase
{
    private string FileName { get; set; } = fileName;
    private static readonly char Slash = OperatingSystem.IsWindows() ? '\\' : '/';
    
    /// <summary>
    /// Lists streams inside the PSS container or extract them
    /// </summary>
    /// <param name="inFile">File stream of the .PSS file</param>
    /// <param name="extract">If true, demux instead of just listing</param>
    /// <param name="outFile">Full path to the output directory</param>
    /// <returns>Table containing info about the PSS container or empty string (when extracting)</returns>
    public string ListPss(Stream inFile, bool extract = false, string? outFile = null)
    {
        outFile ??= Directory.GetCurrentDirectory();
        if (!outFile.EndsWith(Slash))
        {
            outFile += Slash;
        }
        StaticUtils.LiveLoadStatus = "Searching for video/audio streams...";
        Console.Write(StaticUtils.LiveLoadStatus);
        IDictionary<string, long> streams = new Dictionary<string, long>();
        var extractCommands = new List<string>();
        var audioChunks = 0;
        var videoChunks = 0;
        var streamRows = new List<string[]>();
        var relativeOffset = -0x99A0;
        List<string[]> frames = new List<string[]>();
        var frameStarted = false;
        var totalSamples = 0L;
        var totalFrames = 0;
        using (var src = inFile)
        {
            var buffer = new byte[16];

            var offset = 0;
            var seek = 0;
            while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                // audio stream
                if ((buffer[0] == 0x49) && (buffer[1] == 0x4E) && (buffer[2] == 0x54) && (buffer[3] == 0x00))
                {
                    byte[] idbytes = { buffer[4], buffer[5], buffer[6], buffer[7] };
                    byte[] sizebytes = { buffer[8], buffer[9], buffer[10], buffer[11] };
                    byte[] nextpointer = { buffer[12], buffer[13], buffer[14], buffer[15] };
                    var streamID = BitConverter.ToInt32(idbytes, 0);
                    var streamSize = BitConverter.ToInt32(sizebytes, 0);
                    var gotoPointer = BitConverter.ToInt32(nextpointer, 0);
                    var exists = false;
                    var samples = (streamSize / 0x10 * 0xE + ((streamSize % 0x10) - 2));
                    totalSamples += samples;
                    frames.Add(["Audio " + streamID, samples.ToString(), ""]);
                    foreach (var stream in streams.Keys)
                    {
                        if (stream == "Audio " + streamID)
                        {
                            exists = true;
                        }
                    }
                    if (!exists)
                    {
                        streams.Add(new KeyValuePair<string, long>("Audio " + streamID, streamSize));
                    } else
                    {
                        streams["Audio " + streamID] += streamSize;
                    }
                    streamRows.Add([$"0x{relativeOffset:X}", $"0x{seek:X}", $"Audio {streamID}", StaticUtils.GetFilesizeString(gotoPointer), StaticUtils.GetFilesizeString(streamSize), gotoPointer.ToString("X"), streamSize.ToString("X")]);
                    if (extract)
                    {
                        long startRange = seek + 0x10;
                        var endRange = startRange + streamSize - 1;
                        if (File.Exists(outFile + new FileInfo(FileName).Name + $".{streamID}.INT"))
                        {
                            File.Delete(outFile + new FileInfo(FileName).Name + $".{streamID}.INT");
                        }
                        extractCommands.Add(FileName + "," + new FileInfo(FileName).Name + $".{streamID}.INT" + "," + startRange + "," + endRange);
                    }
                    seek += gotoPointer + 0x10;
                    relativeOffset += gotoPointer;
                    src.Seek(seek, 0);
                    audioChunks++;
                    continue;
                }
                // video stream

                if ((buffer[0] == 0x49) && (buffer[1] == 0x50) && (buffer[2] == 0x55) && (buffer[3] == 0x00))
                {
                    byte[] sizebytes = { buffer[8], buffer[9], buffer[10], buffer[11] };
                    byte[] nextpointer = { buffer[12], buffer[13], buffer[14], buffer[15] };
                    var streamSize = BitConverter.ToInt32(sizebytes, 0);
                    var gotoPointer = BitConverter.ToInt32(nextpointer, 0);
                    var shiftRegister = "\0\0\0\0"u8.ToArray();
                    var vFrames = 0;
                    for (var i = 0; i < streamSize; i++)
                    {
                        for (var j = 0; j < 3; j++)
                            shiftRegister[j] = shiftRegister[j + 1];
                        shiftRegister[3] = (byte)src.ReadByte();
                        if ((shiftRegister[0] + shiftRegister[1] == 0) && (shiftRegister[2] == 1) && (shiftRegister[3] == 0xB0))
                        {
                            vFrames++;
                        }
                    }
                    totalFrames += vFrames;
                    frames.Add(["Video", vFrames.ToString(), ""]);
                    streamRows.Add([$"0x{relativeOffset:X}",$"0x{seek:X}", "Video", StaticUtils.GetFilesizeString(gotoPointer), StaticUtils.GetFilesizeString(streamSize), gotoPointer.ToString("X"), streamSize.ToString("X")]);
                    var exists = false;
                    foreach (var stream in streams.Keys)
                    {
                        if (stream == "Video")
                        {
                            exists = true;
                        }
                    }
                    if (!exists)
                    {
                        streams.Add(new KeyValuePair<string, long>("Video", streamSize));
                    }
                    else
                    {
                        streams["Video"] += streamSize;
                    }
                    if (extract)
                    {
                        long startRange = seek + 0x10;
                        var endRange = startRange + streamSize - 1;
                        if (File.Exists(outFile + new FileInfo(FileName).Name + ".IPU"))
                        {
                            File.Delete(outFile + new FileInfo(FileName).Name + ".IPU");
                        }
                        extractCommands.Add(FileName + "," + new FileInfo(FileName).Name + ".IPU" + "," + startRange + "," + endRange);
                    }
                    seek += gotoPointer + 0x10;
                    relativeOffset += gotoPointer;
                    src.Seek(seek, 0);
                    videoChunks++;
                    continue;
                }
                // end of file
                if ((buffer[0] == 0x45) && (buffer[1] == 0x4E) && (buffer[2] == 0x44) && (buffer[3] == 0x00))
                {
                    streamRows.Add([$"0x{relativeOffset:X}", $"0x{seek:X}", "End", StaticUtils.GetFilesizeString(0), StaticUtils.GetFilesizeString(0), "0", "0"]);
                    break;
                }

                seek += 16;
            }
        }
        if (extract)
        {
            StaticUtils.LiveLoadStatus = "Preparing to extract...";
            Console.Write($"\r{StaticUtils.LiveLoadStatus}");
            List<string> OutputFilesList = [];
            foreach (var args in extractCommands.Select(cmd => cmd.Split(',')))
            {
                var outf = outFile + args[1];
                CutFile(args[0], outf, Convert.ToInt64(args[2]), Convert.ToInt64(args[3]));

                if (!OutputFilesList.Contains(outf))
                {
                    OutputFilesList.Add(outf);
                }
            }
            Console.WriteLine("\rThe following streams have been extracted:      ");
            foreach (var outf in OutputFilesList)
            {
                Console.WriteLine(outf);
            }

            return "";
        }
        else
        {
            Console.Write("\r");

            // try to figure out video standard based on how close video duration is 
            // to audio duration based on expected duration calculated with total
            // frame count
            var durationMs = (long)(totalSamples / (streams.Count - 1) / 44100.0 * 1000.0); // based on audio sample count
            var deltaPalMs = Math.Abs((long)(totalFrames / 50.0 * 1000.0) - durationMs); // based on frame count (assuming progressive PAL)
            var deltaNtscMs = Math.Abs((long)(totalFrames / 59.94 * 1000.0) - durationMs); // based on frame count (assuming progressive NTSC)
            var deltaIlPalMs = Math.Abs((long)(totalFrames / 25.0 * 1000.0) - durationMs); // based on frame count (assuming interlaced PAL)
            var deltaIlNtscMs = Math.Abs((long)(totalFrames / 29.97 * 1000.0) - durationMs); // based on frame count (assuming interlaced NTSC)
            var vFormat = "Unknown";
            long[] deltas = [deltaPalMs, deltaIlPalMs, deltaNtscMs, deltaIlNtscMs];
            if (deltas.Min() == deltaPalMs) vFormat = "PAL (progressive scan)";
            else if (deltas.Min() == deltaIlPalMs) vFormat = "PAL (interlaced)";
            else if (deltas.Min() == deltaNtscMs) vFormat = "NTSC (progressive scan)";
            else if (deltas.Min() == deltaIlNtscMs) vFormat = "NTSC (interlaced)";
            if (vFormat.Contains("PAL") && (!StaticUtils.Pal))
            {
                StaticUtils.Pal = true;
            }
            var o = "";
            var sizeRatio = streams["Audio 1"] / (float)streams["Video"] * 100f;
            var chunkRatio = audioChunks/(float)videoChunks*100f;
            o += "Stream summary\n";
            string[] colHeaders = ["Stream", "Size", "Size (hex)"];
            List<string[]> rows = [];
            rows.AddRange(streams.Select(kvp => (string[]) [kvp.Key, StaticUtils.GetFilesizeString(kvp.Value), kvp.Value.ToString("X")]));
            o += StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
            var timeStr = GetMsAsDuration(durationMs).ToString();
            o += $"\nAudio duration: {timeStr}";
            o += $"\nVideo duration: {GetMsAsDuration(durationMs - deltas.Min())}";
            o += $"\nVideo standard: {vFormat}";
            o += $"\nTotal frames: {totalFrames}\n";
            foreach (var (idx, fr) in frames.Index())
            {
                if (fr[0].Contains("Audio"))
                {
                    frames[idx][2] = DotFloatString((float)Math.Round(long.Parse(fr[1]) / 44100.0 * 1000.0, 2)).ToString() + "ms";
                }
                else
                {
                    var divider = 59.94;
                    switch (vFormat)
                    {
                        case "PAL (progressive scan)":
                            divider = 50.0;
                            break;
                        case "PAL (interlaced)":
                            divider = 25.0;
                            break;
                        case "NTSC (progressive scan)":
                            divider = 59.94;
                            break;
                        case "NTSC (interlaced)":
                            divider = 29.97;
                            break;

                    }
                    frames[idx][2] = DotFloatString((float)Math.Round(long.Parse(fr[1]) / divider * 1000.0, 2)).ToString() + "ms";
                }
            }
            o += "\nInterleaving data\n";
            colHeaders = ["Stream", "Fr./Sampl.", "Time"];
            rows.Clear();
            rows.AddRange(frames);
            o += StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput);
            /*
            o += "\nChunk data\n";
            o += $"Video chunks: {videoChunks}, Audio chunks: {audioChunks}\n";
            o += $"Size ratio: {Math.Round(sizeRatio, 2)}%, Chunk ratio: {Math.Round(chunkRatio, 2)}%\n";
            o += $"Multiplier: {Math.Round(chunkRatio/sizeRatio, 2)}x\n";
            colHeaders = ["Relative offset", "Offset", "Stream", "Chunk size", "Buffer size", "Chunk s. (hex)", "Buffer s. (hex)"];
            rows.Clear();
            rows.AddRange(streamRows.ToArray());
            o += StaticUtils.GenerateTable(colHeaders, rows);*/
            rows.Clear();
            return o;
        }
    }

    private static int GetPointer(int streamLength)
    {
        if (streamLength % 0x10 == 0)
        {
            return streamLength;
        }

        return (streamLength / 0x10) * 0x10 + 0x10;
    }

    private static void WriteAudioStream(int streamLength, int streamID, Stream intFile, Stream output, bool ipuHack = false)
    {
        if (!ipuHack)
        {
            output.Write("INT\0"u8);
            output.Write(BitConverter.GetBytes(streamID));
        }
        else
        {
            output.Write("IPU\0"u8);
            output.Write("\0\0\0\0"u8);
        }

        output.Write(BitConverter.GetBytes(streamLength));
        output.Write(BitConverter.GetBytes(GetPointer(streamLength)));

        var start = output.Position;
        var data = new byte[streamLength];
        intFile.ReadExactly(data, 0, streamLength);
        output.Write(data, 0, streamLength);
        output.Position = start + GetPointer(streamLength);
    }

    private static void WriteFrames(int frameCount, Stream ipuFile, Stream output)
    {
        var detectedFrames = 0;
        var shiftRegister = "\0\0\0\0"u8.ToArray();
        while (detectedFrames < frameCount)
        {
            output.Write("IPU\0"u8);
            output.Write("\0\0\0\0"u8);
            output.Write(BitConverter.GetBytes(0x8000));
            output.Write(BitConverter.GetBytes(0x8000));
            var data = new byte[0x8000];
            ipuFile.ReadExactly(data, 0, 0x8000);
            foreach (var t in data)
            {
                for (var j = 0; j < 3; j++)
                    shiftRegister[j] = shiftRegister[j + 1];
                shiftRegister[3] = t;
                if ((shiftRegister[0] + shiftRegister[1] == 0) && (shiftRegister[2] == 1) && (shiftRegister[3] == 0xB0))
                {
                    detectedFrames++;
                }
            }
            output.Write(data);
        }
    }

    // NOTE: Currently only interlaced PAL video is supported
    public static void MergeStreams(FileStream ipuFile, FileStream intFile, FileStream output)
    {
        // beginning
        WriteAudioStream(0x9999, 1, intFile, output);
        WriteFrames(3, ipuFile, output);
        var r = new Random();
        var audioTime = 34405/44100.0;
        var idx = 0;
        while ((ipuFile.Position < ipuFile.Length - 0x8000) && (intFile.Position < intFile.Length - 0x2000))
        {
            WriteAudioStream(0x2000, 1, intFile, output);
            var jitter = idx % 0xC == 0 ? 1 : 0; // This additional frame every Cth write seems to fix lock-ups. The value was brute-forced, so it may still lock up with very long videos.
            var writeFrames = (int)(audioTime / 0.25) + jitter;
            WriteFrames(writeFrames, ipuFile, output);
            if (audioTime > 1.0)
            {
                audioTime -= 1.0;
            }
            idx++;
        }

        WriteAudioStream((int)(intFile.Length - intFile.Position), 1, intFile, output);
        WriteAudioStream((int)(ipuFile.Length - ipuFile.Position), 0, ipuFile, output, true);
        output.Write("END\0"u8);
        output.Write([255, 255, 255, 255]);
        output.Write([255, 255, 255, 255]);
        output.Write([255, 255, 255, 255]); // unknown value, writing 0xFFFFFFFF as a placeholder (will cause a soft-lock when reaching the end of the video)
        output.Close();
    }
    
    private void CutFile(string sourceFilePath, string destinationFilePath, long startPosition, long endPosition) // internal
    {
        StaticUtils.LiveLoadStatus = "Extracting streams, please wait...";
        Console.Write($"\r     {StaticUtils.LiveLoadStatus}".PadRight(StaticUtils.WindowWidth));
        StaticUtils.LoadIdx += 9;
        StaticUtils.PrintLoader();
        const FileMode fm = FileMode.Create;
        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open))
        {
            using (var destinationStream = new FileStream(destinationFilePath + ".TEMP", fm))
            {
                var buffer = new byte[1024];
                int bytesRead;
                // Set the position to the starting position
                sourceStream.Seek(startPosition, SeekOrigin.Begin);

                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (destinationStream.Position + bytesRead > endPosition - startPosition + 1)
                    {
                        // Ensure we don't write more bytes than needed
                        bytesRead = (int)(endPosition - startPosition + 1 - destinationStream.Position);
                    }

                    if (bytesRead <= 0) continue;
                    destinationStream.Write(buffer, 0, bytesRead);

                    if (destinationStream.Position >= endPosition - startPosition + 1)
                    {
                        // Reached the end position, exit the loop
                        break;
                    }
                }
            }
        }
        var fs1 = File.Open(destinationFilePath, FileMode.Append);
        var fs2 = File.Open(destinationFilePath + ".TEMP", FileMode.Open);
        var fs2Content = new byte[fs2.Length];
        fs2.ReadExactly(fs2Content, 0, (int)fs2.Length);
        fs1.Write(fs2Content, 0, (int)fs2.Length);
        fs1.Close();
        fs2.Close();

        File.Delete(destinationFilePath + ".TEMP");
        
    }

}