namespace FlipnicLib;

public abstract class Pss
{
    private static readonly char Slash = OperatingSystem.IsWindows() ? '\\' : '/';
    public static string ListPss(Stream inFile, bool extract = false, string? outFile = null)
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
                        if (File.Exists(outFile + new FileInfo(StaticUtils.FileName).Name + $".{streamID}.INT"))
                        {
                            File.Delete(outFile + new FileInfo(StaticUtils.FileName).Name + $".{streamID}.INT");
                        }
                        extractCommands.Add(StaticUtils.FileName + "," + new FileInfo(StaticUtils.FileName).Name + $".{streamID}.INT" + "," + startRange + "," + endRange);
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
                        if (File.Exists(outFile + new FileInfo(StaticUtils.FileName).Name + ".IPU"))
                        {
                            File.Delete(outFile + new FileInfo(StaticUtils.FileName).Name + ".IPU");
                        }
                        extractCommands.Add(StaticUtils.FileName + "," + new FileInfo(StaticUtils.FileName).Name + ".IPU" + "," + startRange + "," + endRange);
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
            var o = "";
            var sizeRatio = streams["Audio 1"] / (float)streams["Video"] * 100f;
            var chunkRatio = audioChunks/(float)videoChunks*100f;
            o += "Stream summary\n";
            string[] colHeaders = ["Stream", "Size", "Size (hex)"];
            List<string[]> rows = [];
            rows.AddRange(streams.Select(kvp => (string[]) [kvp.Key, StaticUtils.GetFilesizeString(kvp.Value), kvp.Value.ToString("X")]));
            o += StaticUtils.GenerateTable(colHeaders, rows);
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
    
    
    private static void CutFile(string sourceFilePath, string destinationFilePath, long startPosition, long endPosition)
    {
        StaticUtils.LiveLoadStatus = "Extracting streams, please wait...";
        Console.Write($"\r     {StaticUtils.LiveLoadStatus}".PadRight(Console.WindowWidth));
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