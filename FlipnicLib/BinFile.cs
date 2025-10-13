using System.Text;
using FlipnicLib.Types;

namespace FlipnicLib;

public class BinFile
{
    public BinFile() {}
    public List<VirtualFile> FsEntries { get; set; } = [];
    
    /// <summary>
    /// Generate a table containing a list of all the files stored inside the .BIN file
    /// </summary>
    /// <param name="src">The source .BIN file stream</param>
    public void ListBin(Stream src)
    {
        string[] colHeader = ["Path", "Offset", "Size", "TOC offset", "Large buffer"];
        var rows = GetFsEntriesNew(src);
        foreach (var t in rows)
        {
            t[2] = StaticUtils.GetFilesizeString(long.Parse(t[2]));
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
        string[] colHeader = ["Path", "Offset", "Size", "TOC offset"];
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
        var offset = 0;
        long loc = 0;
        long end_of_toc = 9999;
        var intoc = true;
        var pointer = new List<byte> ();
        var insub = false;
        var folder = "";
        long folder_loc = 0;
        List<long> Offsets = [];
        StaticUtils.LiveLoadStatus = "Reading TOC data";
        while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
        {
            string filename;
            var tOff = loc;
            if (intoc)
            {
                if (loc >= end_of_toc)
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
                        end_of_toc = byteoffset;
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
                        folder_loc = loc;
                        StaticUtils.LiveLoadStatus = $"Processing folder {folder}";
                        break;
                    }
                }

                if (filename.EndsWith('\\'))
                {
                    folders[filename] = byteoffset;
                }
                rows.Add([$"\\{filename}", $"0x{byteoffset:X}", $"0x{tOff+64:X}", "Y"]);
                Offsets.Add(byteoffset);
            } else if (insub)
            {
                var i = buffer.Length - 5;
                while (buffer[i] == 0)
                    --i;
                var name = buffer[..(i+1)];
                var soff = buffer[60..];
                filename = Encoding.ASCII.GetString(name);
                var byteoffset = (long)(BitConverter.ToUInt32(soff, 0)) + folder_loc;
                if (filename == "*End Of Mem Data")
                {
                    insub = false;
                    foreach (var kvp in folders.Where(kvp => kvp.Value >= loc))
                    {
                        loc = kvp.Value;
                        src.Position = loc;
                        insub = true;
                        folder = kvp.Key;
                        folder_loc = loc;
                        StaticUtils.LiveLoadStatus = $"Processing {folder}";
                        break;
                    }

                    if (!insub) break;
                    continue;
                }

                rows.Add([$"\\{folder}{filename}", $"0x{byteoffset:X}", $"0x{tOff:X}", "N"]);
                Offsets.Add(byteoffset);
            }
            loc += 64;
        }


        Offsets.Add(src.Length);
        List<long> Sizes = [];
        for (var i = 1; i < Offsets.Count; i++)
        {
            Sizes.Add(Offsets[i] - Offsets[i - 1]);
        }


        List<string[]> realRows = [];
        for (var i = 0; i < Sizes.Count; i++)
        {
            FsEntries.Add(new VirtualFile(rows[i][0], Offsets[i], Sizes[i], Convert.ToInt64(rows[i][2], 16), rows[i][3] == "Y"));
        }
        realRows.AddRange(rows.Select((t, i) => (string[]) [t[0], t[1], Sizes[i].ToString(), t[2], t[3]]));
        return realRows;
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
        Console.Write("\r     Interpreting TOC data...");
        var fsEntries = GetFsEntriesNew(source);
        source.Position = 0;
        using (var src = source)
        {
            Console.Write("\r     Loading file to memory...".PadRight(StaticUtils.WindowWidth));
            for (var i = 0; i < fsEntries.Count; i++)
            {
                var fs_entry = fsEntries[i];
                src.Position = Convert.ToInt64(fs_entry[1], 16);
                var fileNam = fs_entry[0].Replace("\\", "/");
                var end = src.Length;
                if (i < fsEntries.Count - 2)
                {
                    end = Convert.ToInt64(fsEntries[i + 1][1], 16);
                }

                var size = end - src.Position;
                var outFile = Path.Combine(destination, fileNam[1..]);
                Console.Write(
                    $"\r     Extracting {fileNam} ({StaticUtils.GetFilesizeString(size)})".PadRight(StaticUtils.WindowWidth));
                if (fileNam.EndsWith('/')) continue;
                if (size < 0) continue;
                src.Position = Convert.ToInt64(fs_entry[1], 16);
                var bufSize = (int)((size % 0x800 != 0) ? size : 0x800);
                if (i < fsEntries.Count - 2)
                {
                    end = Convert.ToInt64(fsEntries[i + 1][1], 16);
                }
                if (!Directory.Exists(new FileInfo(outFile).Directory.FullName))
                {
                    Directory.CreateDirectory(new FileInfo(outFile).Directory.FullName);
                }

                using var fs = File.OpenWrite(Path.Combine(destination, fileNam[1..]));
                for (var j = 0; j < size; j += bufSize)
                {
                    var buffer = new byte[bufSize];
                    src.ReadExactly(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, buffer.Length);
                    if (j % 0x4000 == 0) StaticUtils.PrintLoader();
                }

                fs.Close();
            }
        }
        Console.WriteLine($"\r   Files have been extracted to: {destination}".PadRight(StaticUtils.WindowWidth));

    }
}