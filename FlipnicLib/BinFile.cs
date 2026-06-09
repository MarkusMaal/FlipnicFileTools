using System.Text;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicLib;

public class BinFile : FormatBase
{
    public List<VirtualFile> FsEntries { get; set; } = [];

    /// <summary>
    /// Generate a table containing a list of all the files stored inside the .BIN file
    /// </summary>
    /// <param name="src">The source .BIN file stream</param>
    /// <param name="noDisplay">Only get filesystem entries, don't display anything</param>
    public void ListBin(Stream src, bool noDisplay = false)
    {
        string[] colHeader = ["Path", "Offset", "Size", "TOC offset", "Large buffer"];
        var rows = GetFsEntriesNew(src);
        if (noDisplay) return;
        foreach (var t in rows)
        {
            t[2] = GetFilesizeString(long.Parse(t[2]));
        }
        src.Close();
        Console.Write(StaticUtils.GenerateTable(colHeader, rows, StaticUtils.SimpleOutput));
    }
    
    /// <summary>
    /// Generate a VirtualFile array containing all the files stored inside the .BIN file
    /// </summary>
    /// <param name="src">The source .BIN file stream</param>
    /// <returns>An array, where each entry contains virtual file path, offset and size</returns>
    public VirtualFile[] GetListBin(Stream src)
    {
        var rows = GetFsEntriesNew(src);
        var fsEntries = rows.Select(row => new VirtualFile(row[0], Convert.ToInt64(row[1], 16), long.Parse(row[2]), Convert.ToInt64(row[3], 16), row[4] == "Y")).ToList();
        src.Close();
        return fsEntries.ToArray();
    }

    // internal method for getting the VFS entries
    private List<string[]> GetFsEntriesNew(Stream src)
    {
        
        FsEntries.Clear();
        List<string[]> rows = [];
        var folders = new Dictionary<string, long>();
        var buffer = new byte[64];
        long loc = 0;
        long endOfToc = 9999;
        var intoc = true;
        var pointer = new List<byte> ();
        var insub = false;
        var folder = "";
        long folderLoc = 0;
        List<long> offsets = [];
        StaticUtils.LiveLoadStatus = "Reading TOC data";
        while (src.Read(buffer, 0, buffer.Length) > 0)
        {
            string filename;
            var tOff = loc;
            var perc = Math.Round(src.Position / (double)src.Length * 100.0);
            if (intoc)
            {
                if (loc >= endOfToc)
                {
                    intoc = false;
                    if (folders.Count == 0) break;
                    continue;
                }
                pointer.Clear();
                filename = buffer[..60].Where(b => b != 0x00).Aggregate("", (current, b) => current + Encoding.ASCII.GetString([b]));
                var bytes = buffer[60..];
                var byteoffset = (long)(BitConverter.ToInt32(bytes, 0)) * 2048;
                switch (filename)
                {
                    case "*Top Of CD Data":
                        endOfToc = byteoffset;
                        continue;
                    case "*End Of CD Data":
                        intoc = false;
                        break;
                }

                if (!intoc)
                {
                    if (folders.Count == 0) break;
                    StaticUtils.LiveLoadStatus = "Searching for folders...";
                    foreach (var kvp in folders.Where(kvp => loc < kvp.Value))
                    {
                        loc = kvp.Value;
                        src.Seek(loc, SeekOrigin.Begin);
                        insub = true;
                        folder = kvp.Key;
                        folderLoc = loc;
                        StaticUtils.LiveLoadStatus = $"Processing folder {folder} ({perc}%)";
                        break;
                    }
                }

                if (filename.EndsWith('\\'))
                {
                    folders[filename] = byteoffset;
                }
                rows.Add([$"\\{filename}", $"0x{byteoffset:X}", $"0x{tOff+64:X}", "Y"]);
                offsets.Add(byteoffset);
            } else if (insub)
            {
                tOff -= 0x40; // there is no "*Top Of" file for subdirs, so subtract 0x40 to get actual TOC offset 
                var i = buffer.Length - 5;
                while (buffer[i] == 0)
                    --i;
                var name = buffer[..(i+1)];
                var soff = buffer[60..];
                filename = Encoding.ASCII.GetString(name);
                var byteoffset = BitConverter.ToUInt32(soff, 0) + folderLoc;
                if (filename == "*End Of Mem Data")
                {
                    insub = false;
                    foreach (var kvp in folders.Where(kvp => kvp.Value >= loc))
                    {
                        loc = kvp.Value;
                        src.Position = loc;
                        insub = true;
                        folder = kvp.Key;
                        folderLoc = loc;
                        StaticUtils.LiveLoadStatus = $"Processing {folder} ({perc}%)";
                        break;
                    }

                    if (!insub) break;
                    continue;
                }

                rows.Add([$"\\{folder}{filename}", $"0x{byteoffset:X}", $"0x{tOff:X}", "N"]);
                offsets.Add(byteoffset);
            }
            loc += 64;
        }


        offsets.Add(src.Length);
        List<long> sizes = [];
        for (var i = 1; i < offsets.Count; i++)
        {
            sizes.Add(offsets[i] - offsets[i - 1]);
        }


        List<string[]> realRows = [];
        for (var i = 0; i < sizes.Count; i++)
        {
            StaticUtils.LiveLoadStatus = "Populating file entries... (" + Math.Round(i / (double)sizes.Count * 100.0) + "%)";
            FsEntries.Add(new VirtualFile(rows[i][0], offsets[i], sizes[i], Convert.ToInt64(rows[i][2], 16), rows[i][3] == "Y"));
        }

        StaticUtils.LiveLoadStatus = "Processing...";
        realRows.AddRange(rows.Select((t, i) => (string[]) [t[0], t[1], sizes[i].ToString(), t[2], t[3]]));
        return realRows;
    }

    /// <summary>
    /// Extracts all subfolders as PAK files from the .BIN file (useful for modding purposes)
    /// </summary>
    /// <param name="source">BIN file stream</param>
    /// <param name="destination">Full path to the folder to extract the PAK files to</param>
    public void ExtractPak(Stream source, string destination)
    {
        Console.Write("\r     Interpreting TOC data...");
        var fsEntries = GetFsEntriesNew(source);
        
        source.Position = 0;
        var count = 0;
        using (var src = source)
        {
            Console.Write("\r     Loading file to memory...".PadRight(StaticUtils.WindowWidth));
            for (var i = 0; i < fsEntries.Count; i++)
            {
                var fsEntry = fsEntries[i];
                src.Position = Convert.ToInt64(fsEntry[1], 16);
                var fileNam = fsEntry[0].Replace("\\", "/");
                if (!fileNam.EndsWith('/')) continue; // not a subdirectory, ignore those entries
                count++;
                fileNam = fileNam[..^1] + ".PAK"; // e.g. BOSS1\ -> BOSS1.PAK
                var end = src.Length;
                if (i < fsEntries.Count - 2)
                {
                    end = Convert.ToInt64(fsEntries[i + 1][1], 16);
                }

                var size = end - src.Position;
                var outFile = Path.Combine(destination, fileNam[1..]);
                using (var os = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[2048];
                    for (var j = src.Position; j < end; j += 2048)
                    {
                        src.ReadExactly(buffer, 0, 2048);
                        os.Write(buffer, 0, 2048);
                    }
                }

                Console.Write(
                    $"\r     Extracting {fileNam} ({GetFilesizeString(size)})".PadRight(StaticUtils.WindowWidth));
                if (size < 0) continue;
                src.Position = Convert.ToInt64(fsEntry[1], 16);
            }

            src.Close();
        }

        if (count == 0)
        {
            Console.WriteLine("\r   This .BIN file does not contain any subdirectories!");
            return;
        }
        Console.WriteLine("\r   " + (count == 1 ? "A file has" : "Files have") + $" been extracted to: {destination}".PadRight(StaticUtils.WindowWidth));
    }

    /// <summary>
    /// For PAK container manipulation
    /// </summary>
    /// <param name="source">Source stream. Either a file or memory stream. Former is recommended.</param>
    /// <param name="replace">If true, replace a file inside the container instead of showing a list of files.</param>
    /// <param name="replacementName">Name of the file inside the container we want to replace.</param>
    /// <param name="replacement">Replacement file stream.</param>
    public void ListPak(Stream source, bool replace = false, string? replacementName = null, Stream? replacement = null)
    {
        // listing files
        var buffer = new byte[0x40];
        var colHeaders = new[] { "Name", "Offset", "Size" };
        var rows = new List<string[]>();
        var offsets = new List<long>();
        var sizes = new List<long>();
        var walks = 0;
        while (walks < 32768)
        {
            walks++;
            try
            {
                source.ReadExactly(buffer, 0, 0x40);
            }
            catch (EndOfStreamException)
            {
                StaticUtils.DecodeColors( "~-CError~--: End of stream reached while traversing table of contents!");
                Console.WriteLine();
                return;
            }

            var name = GetString(buffer);
            var offset = GetUInt32(buffer, 0x3C);
            if (rows.Count > 0)
            {
                // calculate the file size for previous entry based on current entry's offset
                // this works, because there is an extra "*End Of Mem Data", which has the
                // total size of the container as its offset
                var prevSize = offset - offsets[^1];
                sizes.Add(prevSize);
                rows[^1][2] = StaticUtils.GetFilesizeString(prevSize);
            }
            offsets.Add(offset);
            if (name == "*End Of Mem Data") break; // don't add end pointer to the list of files
            rows.Add([name, "0x" + offset.ToString("X"), ""]);
        }

        if (walks == 32768)
        {
            StaticUtils.DecodeColors( "~-CError~--: Cannot find the end pointer, the PAK file may be corrupt or incompatible!");
            Console.WriteLine();
            return;
        }

        if (!replace || replacementName == null || replacement == null)
        {
            Console.Write(StaticUtils.GenerateTable(colHeaders, rows, StaticUtils.SimpleOutput));
            Console.WriteLine("End offset: 0x" + offsets[^1].ToString("X"));
            return;
        }

        if (!source.CanWrite)
        {
            StaticUtils.DecodeColors( "~-CError~--: Cannot write to this file");
            Console.WriteLine();
            return;
        }
        
        // repacking

        var idx = -1;
        foreach (var (i, row) in rows.Index()) // find the file in TOC
        {
            if (row[0] != replacementName) continue;
            idx = i;
            break;
        }

        if (idx == -1)
        {
            StaticUtils.DecodeColors( $"~-CError~--: The specified virtual file ({replacementName}) does not exist!");
            Console.WriteLine();
            return;
        }

        var sizeDelta = replacement.Length - sizes[idx];
        var replacementOffset = offsets[idx];
        var replacementSize = replacement.Length;

        if (sizeDelta != 0) // when the size is equal, just write the data, no need to worry about any of this
        {
            Console.WriteLine("Updating TOC offsets");
            // + 2 is due to the following reasons:
            //      1) The modified entry itself (we don't want to change that)
            //      2) 0x40 * 0 - 0x4 would be a negative value, so the index actually starts with 1
            // we are also changing "*End Of Mem Data" pointer
            for (var i = idx + 2; i <= rows.Count + 1; i++)
            {
                source.Position = 0x40 * i - 0x4;
                var buff = BitConverter.GetBytes((uint)(offsets[i-1] + sizeDelta));
                source.Write(buff, 0, 4);
            }
            if (sizeDelta > 0) // expand container (when replacement file is bigger than the original)
            {
                for (var i = offsets[^1]; i > replacementOffset + sizeDelta; i--)
                {
                    source.Position = i;
                    var originalByte = source.ReadByte();
                    source.Position = i + sizeDelta;
                    source.WriteByte((byte)originalByte);
                    if (i % 2048 != 0) continue;
                    Console.Write("Moving data to make room (" + (int)Math.Max(0,
                                      Math.Round((110 - (i / (double)(replacementOffset + sizeDelta) * 100.0)) * 10)) + // this calculation is just some spaghetti code, but it works, so I don't touch it lol
                                  "% complete)\r");
                }
            }
            else // shrink container (when replacement file is smaller than the original)
            {
                for (var i = replacementOffset; i < offsets[^1] + sizeDelta; i++)
                {
                    source.Position = i - sizeDelta;
                    var originalByte = source.ReadByte();
                    source.Position = i;
                    source.WriteByte((byte)originalByte);
                    if (i % 2048 != 0) continue;
                    Console.Write("Shrinking file (" + (int)Math.Max(0,
                                      Math.Round(((i / (double)(replacementOffset + sizeDelta) * 100.0)) * 10) - 1002.0) + // same story as the last percentage calculation
                                  "% complete)\r");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("Writing new data");
        // a simple byte-by-byte copy (works just fine for tiny containers)
        for (var i = replacementOffset; i < replacementOffset + replacementSize; i++)
        {
            source.Position = i;
            source.WriteByte((byte)replacement.ReadByte());
        }
        
        // when extracting a file from a BIN container, it's likely going to have some padding at the end (up to 2047 bytes)
        // we want to remove that, especially when we are increasing the file size
        // uses "*End Of Mem Data" pointer from earlier
        Console.WriteLine("Trimming end padding");
        source.SetLength(offsets[^1] + sizeDelta);
        
        // can't show the filename when we are writing to a memory stream
        if (source is not FileStream fStr) return;
        StaticUtils.DecodeColors("~-ASuccess~--: Changes have been written to " + fStr.Name);
        Console.WriteLine();
    }
    
    /// <summary>
    /// Extracts all files inside the .BIN container
    /// </summary>
    /// <param name="source">The input .BIN file stream</param>
    /// <param name="destination">Full path to the folder to extract the files to</param>
    public void ExtractBin(Stream source, string destination)
    {
        if (Directory.Exists(destination))
        {
            Console.Write("Specified folder already exists. Overwrite? [Y/N] ");
            var result = Console.ReadKey().Key;
            while (result is not (ConsoleKey.Y or ConsoleKey.N))
            {
                result = Console.ReadKey().Key;
            }
            if (result is ConsoleKey.Y or ConsoleKey.N)
            {
                Console.Write("\n");
            }
        }

        StaticUtils.LiveLoadStatus = "Interpreting TOC data...";
        var fsEntries = GetFsEntriesNew(source);
        source.Position = 0;
        using (var src = source)
        {
            StaticUtils.LiveLoadStatus = "Loading file to memory...";
            for (var i = 0; i < fsEntries.Count; i++)
            {
                var fsEntry = fsEntries[i];
                src.Position = Convert.ToInt64(fsEntry[1], 16);
                var fileNam = fsEntry[0].Replace("\\", "/");
                var end = src.Length;
                if (i < fsEntries.Count - 2)
                {
                    end = Convert.ToInt64(fsEntries[i + 1][1], 16);
                }

                var size = end - src.Position;
                var outFile = Path.Combine(destination, fileNam[1..]);
                StaticUtils.LiveLoadStatus = $"Extracting {fileNam} ({GetFilesizeString(size)})";
                if (fileNam.EndsWith('/')) continue;
                if (size < 0) continue;
                src.Position = Convert.ToInt64(fsEntry[1], 16);
                var bufSize = (int)((size % 0x800 != 0) ? size : 0x800);
                if (i < fsEntries.Count - 2)
                {
                    end = Convert.ToInt64(fsEntries[i + 1][1], 16);
                }
                if (!Directory.Exists(new FileInfo(outFile).Directory!.FullName))
                {
                    Directory.CreateDirectory(new FileInfo(outFile).Directory!.FullName);
                }

                using var fs = File.OpenWrite(Path.Combine(destination, fileNam[1..]));
                for (var j = 0; j < size; j += bufSize)
                {
                    var buffer = new byte[bufSize];
                    src.ReadExactly(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, buffer.Length);
                    if (j % 0x4000 == 0) StaticUtils.LiveLoadStatus = $"Extracting {fileNam} ({GetFilesizeString(size)})";
                }

                fs.Close();
            }
        }

        StaticUtils.LiveLoadStatus = "";
        Console.WriteLine($"\r   Files have been extracted to: {destination}".PadRight(StaticUtils.WindowWidth));

    }
}