using System.Text.Json;
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
        return [.. fsEntries];
    }

    private static Dictionary<TocEntry, TocEntry[]> GetTocData(Stream src)
    {
        var tocEntries = new Dictionary<TocEntry, TocEntry[]>();
        // read each file entry until we reach the "*End Of CD Data" segment
        while (true)
        {
            var buffer = new byte[0x40];
            src.ReadExactly(buffer, 0, buffer.Length);
            var currentEntry = TocEntry.FromBytes(buffer);
            var fnStr = new string(currentEntry.FileName);
            List<TocEntry> subEntries = [];
            if (fnStr.EndsWith('\\'))
            {
                var returnPos = src.Position;
                src.Position = currentEntry.Offset * 0x800;
                while (true)
                {
                    src.ReadExactly(buffer, 0, buffer.Length);
                    var currentSubEntry = TocEntry.FromBytes(buffer);
                    subEntries.Add(currentSubEntry);
                    var subFnStr = new string(currentSubEntry.FileName);
                    if (subFnStr == "*End Of Mem Data") break;
                }

                src.Position = returnPos;
            }

            while (tocEntries.ContainsKey(currentEntry))
            {
                currentEntry.FileName = (currentEntry + "_").ToCharArray();
            }
            tocEntries.Add(currentEntry, [.. subEntries]);
            if (fnStr.Equals("*End Of CD Data")) break;
        }

        return tocEntries;
    }

    // internal method for getting the VFS entries
    private List<string[]> GetFsEntriesNew(Stream src)
    {
        var tocEntries = GetTocData(src);
        StaticUtils.LiveLoadStatus = "Reading TOC data";
        List<string[]> rows = [];
        StaticUtils.LiveLoadStatus = "Processing TOC data";
        // turn the read data into something the rest of the code here can understand
        foreach (var (i, (entry, subEntries)) in tocEntries.Index().Skip(1))
        {
            var byteOffset = entry.Offset * 0x800;
            long tOff = i * 0x40;
            long size;
            if (!entry.FileName.StartsWith("*End Of CD Data"))
            {
                size = 0x800 * (tocEntries.Keys.ToArray()[i + 1].Offset - entry.Offset);
            }
            else
            {
                break;
            }
            rows.Add([$"\\{new string(entry.FileName)}", $"0x{byteOffset:X}", $"{size}" , $"0x{tOff+64:X}", "Y"]);
            foreach (var (j, subEntry) in subEntries.Index())
            {
                byteOffset = entry.Offset * 0x800 + subEntry.Offset;
                tOff = entry.Offset * 0x800 + i * 0x40;
                if (!subEntry.FileName.StartsWith("*End Of Mem Data"))
                {
                    size = (subEntries.ToArray()[j + 1].Offset - subEntry.Offset);
                }
                else
                {
                    continue;
                }
                rows.Add([$@"\{new string(entry.FileName)}{new string(subEntry.FileName)}", $"0x{byteOffset:X}", $"{size}" , $"0x{tOff+64:X}", "N"]);
            }
        }
        
        StaticUtils.LiveLoadStatus = "Generating virtual file array";
        // for file table in GUI
        FsEntries.Clear();
        for (var i = 0; i < rows.Count; i++)
        {
            StaticUtils.LiveLoadStatus = "Populating file entries... (" + Math.Round(i / (double)rows.Count * 100.0) + "%)";
            FsEntries.Add(new VirtualFile(rows[i][0], Convert.ToInt64(rows[i][1], 16), Convert.ToInt64(rows[i][2]), Convert.ToInt64(rows[i][3], 16), rows[i][4] == "Y"));
        }
        return rows;
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
                StaticUtils.DecodeColors( "~-CError~--\a: End of stream reached while traversing table of contents!");
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
            StaticUtils.DecodeColors( "~-CError~--\a: Cannot find the end pointer, the PAK file may be corrupt or incompatible!");
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
            StaticUtils.DecodeColors( "~-CError~--\a: Cannot write to this file");
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
            StaticUtils.DecodeColors( $"~-CError~--\a: The specified virtual file ({replacementName}) does not exist!");
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
        StaticUtils.DecodeColors("~-ASuccess\a~--: Changes have been written to " + fStr.Name);
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
            while (result is not (ConsoleKey.Y or ConsoleKey.N)) result = Console.ReadKey().Key;
            if (result is ConsoleKey.Y or ConsoleKey.N) Console.Write("\n");
            if (result == ConsoleKey.N) return;
        }
        else
        {
            Directory.CreateDirectory(destination);
        }
        var infoFile = Path.Join(destination, "metadata.json");
        var infoStream = File.CreateText(infoFile);
        var infoData = new DirInfo();
        StaticUtils.LiveLoadStatus = "Interpreting TOC data...";
        var fsEntries = GetTocData(source);

        foreach (var (i, (entry, subEntries)) in fsEntries.Index().Skip(1))
        {
            var entryName = new string(entry.FileName).TrimEnd('\0');
            if (entryName.StartsWith("*End Of CD Data")) break;
            if (!entryName.EndsWith('\\'))
            {
                var sizeExplicit = (0x800) * (fsEntries.Keys.ToArray()[i + 1].Offset - entry.Offset);
                
                if (entryName.Contains('\\')) infoData.AppendLb(entryName);
                source.Position = 0x800 * entry.Offset;
                var localEntryName = entryName;
                if (!OperatingSystem.IsWindows()) localEntryName = localEntryName.Replace('\\', '/');
                var outPath = Path.Join(destination, localEntryName);
                var outStream = File.OpenWrite(outPath);
                var buffer = new byte[0x800];
                for (var x = 0; x < sizeExplicit; x += 0x800)
                {
                    if (x % 0x4000 == 0)
                        StaticUtils.LiveLoadStatus = $"Extracting {entryName} ({GetFilesizeString(sizeExplicit)})";
                    source.ReadExactly(buffer, 0, buffer.Length);
                    outStream.Write(buffer, 0, buffer.Length);
                }

                outStream.Close();
            }
            else
            {
                var baseOffset = entry.Offset * 0x800;
                var baseDir = Path.Join(destination, entryName.Replace("\\", ""));
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
                infoData.AppendEntry(entryName);
                foreach (var (j, sE) in subEntries.Index())
                {
                    var subEntryName = new string(sE.FileName).TrimEnd('\0');
                    if (subEntryName.StartsWith("*End Of Mem Data")) break;
                    var sizeExplicit = subEntries[j + 1].Offset - sE.Offset;
                    StaticUtils.LiveLoadStatus = $"Extracting {entryName}{subEntryName} ({GetFilesizeString(sizeExplicit)})";
                    var outStream = File.OpenWrite(Path.Join(baseDir, subEntryName));
                    var buffer = new byte[sizeExplicit];
                    source.Position = baseOffset + sE.Offset;
                    source.ReadExactly(buffer, 0, (int)sizeExplicit);
                    outStream.Write(buffer, 0, (int)sizeExplicit);
                    outStream.Close();
                }
            }
        }
        infoStream.Write(JsonSerializer.Serialize(infoData, DirInfoGenerationContext.Default.DirInfo));
        infoStream.Close();
        StaticUtils.LiveLoadStatus = "";
        Console.WriteLine($"\r   Files have been extracted to: {destination}".PadRight(StaticUtils.WindowWidth));
    }
    
    public static void GenerateBin(string source, Stream destination)
    {
        if (!File.Exists(Path.Join(source, "metadata.json")))
        {
            throw new FileNotFoundException("\"metadata.json\" doesn't exist! If you extracted the BIN file with an older version of Flipnic File Tools, re-extract it with this version.");
        }
        StaticUtils.LiveLoadStatus = "Reading metadata.json...";
        var metaFile = File.OpenText(Path.Join(source, "metadata.json"));
        var metaJson = metaFile.ReadToEnd();
        var meta = JsonSerializer.Deserialize(metaJson, DirInfoGenerationContext.Default.DirInfo);
        if (meta == null) return;
        var tocEnd = 0x80 + (uint)meta.Entries.Length * 0x40; // subdirectories and delimiters
        tocEnd = meta.Entries.Aggregate(tocEnd, (current, lb) => current + 0x40 * (uint)lb.LargeBuffers.Length); // large buffer hacks
        var allFilesEnumerable = Directory.EnumerateFiles(source, "*.*", SearchOption.TopDirectoryOnly);
        var allFilesArray = allFilesEnumerable as string[] ?? [.. allFilesEnumerable];
        tocEnd += allFilesArray.Where(f => f != "metadata.json").Aggregate(tocEnd, (current, _) => current + 0x40); // top level files

        while (tocEnd % 0x800 != 0)
        {
            tocEnd++;
        }
        
        // identify end of TOC
        destination.Write(new TocEntry
        {
            FileName = "*Top Of CD Data".ToCharArray(),
            Offset = tocEnd / 0x800
        }.GetBytes());

        var tocOffset = 0x40;
        var fileOffset = tocEnd;
        var watchout = "";
        if (destination is FileStream ffs) watchout = ffs.Name; // so that we don't write the BIN file we are currently writing inside the BIN file we are writing (l o g i c)
        // write subdirectories
        foreach (var entry in meta.Entries)
        {
            if (!Directory.Exists(Path.Join(source, entry.Directory.Replace("\\", ""))))
            {
                StaticUtils.DecodeColors($"~-EWarning~--: Directory {entry.Directory}, which is specified by metadata.json doesn't exist");
                Console.WriteLine();
                continue;
            }
            destination.Write(new TocEntry
            {
                FileName = entry.Directory.ToCharArray(),
                Offset = fileOffset / 0x800,
            }.GetBytes());
            tocOffset += 0x40;
            destination.Position = fileOffset;
            var genDir = GenerateFolder(Path.Join(source, entry.Directory.Replace("\\", "")), entry.LargeBuffers, watchout);
            StaticUtils.LiveLoadStatus = $"Packing {entry.Directory}";
            destination.Write(genDir);
            fileOffset += (uint)genDir.Length;
            while (fileOffset % 0x800 != 0) fileOffset++;
            destination.Position = tocOffset;
            foreach (var lb in entry.LargeBuffers)
            {
                if (!File.Exists(Path.Join(source, entry.Directory.Replace("\\", ""), lb)))
                {
                    StaticUtils.DecodeColors($"~-EWarning~--: File {entry.Directory}{lb}, which is specified by metadata.json doesn't exist");
                    Console.WriteLine();
                    continue;
                }
                destination.Write(new TocEntry
                {
                    FileName = (entry.Directory + lb).ToCharArray(),
                    Offset = fileOffset / 0x800
                }.GetBytes());
                tocOffset += 0x40;
                destination.Position = fileOffset;
                var aFile = new FileInfo(Path.Join(source, entry.Directory.Replace("\\", ""), lb));
                if (aFile.Length > 512 * 1024)
                {
                    StaticUtils.DecodeColors($"~-EWarning~--: File {entry.Directory}{lb} is over 512kiB, which may cause crashes");
                    Console.WriteLine();
                }
                ReadFileToStream(aFile.FullName, destination);
                fileOffset += (uint)aFile.Length;
                while (fileOffset % 0x800 != 0) fileOffset++;
                destination.Position = tocOffset;
            }
        }
        
        // write top level files
        foreach (var fullFile in allFilesArray)
        {
            if (fullFile == watchout) continue;
            var file = new FileInfo(fullFile).Name;
            if (file.Length == 0) continue; // .DS_Store, etc.
            if ((new FileInfo(fullFile).Attributes & FileAttributes.Hidden) != 0) continue; // desktop.ini, Thumbs.db etc.
            if (file == "metadata.json") continue;
            destination.Position = tocOffset;
            destination.Write(new TocEntry
            {
                FileName = file.ToCharArray(),
                Offset = fileOffset / 0x800,
            }.GetBytes());
            tocOffset += 0x40;
            destination.Position = fileOffset;
            var aFile = new FileInfo(Path.Join(source, file));
            ReadFileToStream(aFile.FullName, destination);
            fileOffset += (uint)(aFile.Length);
            while (fileOffset % 0x800 != 0) fileOffset++;
        }
        destination.Position = tocOffset;
        
        // write end offset
        destination.Write(new TocEntry
        {
            FileName = "*End Of CD Data".ToCharArray(),
            Offset = fileOffset / 0x800
        }.GetBytes());
        StaticUtils.LiveLoadStatus = "Padding";
        while (destination.Length % 0x800 != 0)
        {
            destination.SetLength(destination.Length + 1);
        }
        destination.Close();
        StaticUtils.LiveLoadStatus = "";
    }

    private static void ReadFileToStream(string fullName, Stream destination)
    {
        var file = new FileInfo(fullName).Name;
        var inFile = File.OpenRead(fullName);
        var buffer = new byte[0x800];
        for (var i = 0; i < inFile.Length; i+=0x800)
        {
            StaticUtils.LiveLoadStatus = $"Packing {file}";
            if (i + 0x800 > inFile.Length)
            {
                buffer = new byte[inFile.Length - i];
            }
            inFile.ReadExactly(buffer, 0, buffer.Length);
            destination.Write(buffer);
        }
        inFile.Close();
    }

    private static byte[] GenerateFolder(string source, string[] exclusions, string watchout)
    {
        var ms = new MemoryStream();
        uint tocOffset = 0;
        var tocLength = 0x40 * (uint)Directory.EnumerateFiles(source).ToArray().Length - (uint)(0x40 * exclusions.Length) + 0x40;
        var offset = tocLength;
        var parent = new DirectoryInfo(source).Name;
        foreach (var fullPath in Directory.EnumerateFiles(source))
        {
            if (fullPath == watchout) continue;
            var f = new FileInfo(fullPath).Name;
            if (f.Length == 0) continue; // .DS_Store, etc.
            if ((new FileInfo(fullPath).Attributes & FileAttributes.Hidden) != 0) continue; // Desktop.ini, etc.
            var skip = false;
            foreach (var excl in exclusions)
            {
                if (f == excl) skip = true;
            }

            if (skip) continue;
            var fullSourceFile = new FileInfo(Path.Join(source, f));
            ms.Write(new TocEntry
            {
                FileName = f.ToCharArray(),
                Offset = offset
            }.GetBytes());
            tocOffset += 0x40;
            ms.Position = offset;
            var fs = File.OpenRead(fullSourceFile.FullName);
            if (fs.Length > 0x30 && fs.Length % 0x800 == 0)
            {
                fs.Position = fs.Length - 0x31;
                var testByte = 0;
                for (var i = 0; i < 0x30; i++) // read 0x30 bytes to eliminate false positives
                {
                    testByte += fs.ReadByte();
                }

                fs.Position = 0;
                if (testByte == 0)
                {
                    StaticUtils.DecodeColors($"~-EWarning~--: {parent}\\{f} may have unwanted end padding");
                    Console.WriteLine();
                }
            }
            for (var i = 0; i < fs.Length; i++)
            {
                if (i % 0x1000 == 0) StaticUtils.LiveLoadStatus = $"Generating {parent}\\{f}";
                ms.WriteByte((byte)fs.ReadByte());
            }

            fs.Close();
            ms.Position = tocOffset;
            offset += (uint)fullSourceFile.Length; 
        }
        ms.Write(new TocEntry
        {
            FileName = "*End Of Mem Data".ToCharArray(),
            Offset = offset
        }.GetBytes());
        StaticUtils.LiveLoadStatus = $"Packing {parent}\\";
        ms.Position = 0;
        var outArray = ms.ToArray();
        ms.Close();
        return outArray;
    }

    private struct TocEntry : IEquatable<TocEntry>
    {
        public char[] FileName { get; set; }
        public uint Offset { get; init; }

        public byte[] GetBytes()
        {
            var data = new byte[0x40];
            foreach (var (i, c) in FileName.Index())
            {
                data[i] = (byte)c;
            }

            var offset = BitConverter.GetBytes(Offset);
            data[0x3C] = offset[0];
            data[0x3D] = offset[1];
            data[0x3E] = offset[2];
            data[0x3F] = offset[3];
            return data;
        }

        public static TocEntry FromBytes(byte[] data)
        {
            return new TocEntry
            {
                FileName = GetString([.. data.Take(0x3C)]).ToCharArray(),
                Offset = BitConverter.ToUInt32([.. data.Skip(0x3C).Take(4)]),
            };
        }

        public bool Equals(TocEntry other)
        {
            return FileName.Equals(other.FileName) && Offset == other.Offset;
        }

        public override bool Equals(object? obj)
        {
            return obj is TocEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, Offset);
        }
    }
}