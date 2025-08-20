using System.Text;

namespace FlipnicLib;

public abstract class BinFile
{
    
    public static void ListBin(string source)
    {
        string[] colHeader = ["Path", "Offset", "Size"];
        List<string[]> rows = [];
        var folders = new Dictionary<string, long>();
        using Stream src = File.OpenRead(source);
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
        while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
        {
            string filename;
            if (intoc)
            {
                if (loc == end_of_toc)
                {
                    intoc = false;
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
                        continue;
                }

                if (filename.EndsWith('\\'))
                {
                    folders[filename] = byteoffset;
                }
                rows.Add([$"\\{filename}", $"0x{byteoffset:X}"]);
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
                } else
                {
                    rows.Add([$"\\{folder}{filename}", $"0x{byteoffset:X}"]);
                    Offsets.Add(byteoffset);
                }
            }
            else
            {
                foreach (var kvp in folders.Where(kvp => kvp.Value == loc))
                {
                    insub = true;
                    folder = kvp.Key;
                    folder_loc = kvp.Value;
                    var i = buffer.Length - 5;
                    while (buffer[i] == 0)
                        --i;
                    var name = buffer[..(i + 1)];
                    var soff = buffer[60..];
                    filename = Encoding.ASCII.GetString(name);
                    var byteoffset = BitConverter.ToUInt32(soff, 0) + kvp.Value;
                    rows.Add([$"\\{kvp.Key}{filename}", $"0x{byteoffset:X}"]);
                    Offsets.Add(byteoffset);
                }
            }

            loc += 64;
        }

        Offsets.Add(new FileInfo(source).Length);
        List<long> Sizes = [];
        for (var i = 1; i < Offsets.Count; i++)
        {
            Sizes.Add(Offsets[i] - Offsets[i - 1]);
        }

        List<string[]> realRows = [];
        realRows.AddRange(rows.Select((t, i) => (string[]) [t[0], t[1], StaticUtils.GetFilesizeString(Sizes[i])]));
        Console.Write(StaticUtils.GenerateTable(colHeader, realRows,
            realRows.Select(row => row[0].Length + 1).Prepend(15).Max()));
    }


    private static Dictionary<string, long> GetFsEntries(string source)
    {
        var fsentries = new Dictionary<string, long>();
        using Stream src = File.OpenRead(source);
        var buffer = new byte[64];
        var filename = "";
        var offset = 0;
        long loc = 0;
        long end_of_toc = 9999;
        var intoc = true;
        var pointer = new List<byte>();
        while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
        {
            var cache = new byte[buffer.Length];
            Buffer.BlockCopy(buffer, 0, cache, 0, buffer.Length);
            if (intoc)
            {
                filename = "";
                if (loc == end_of_toc)
                {
                    intoc = false;
                    continue;
                }
                pointer.Clear();
                foreach (var b in cache[..60])
                {
                    if (b == 0x00)
                    {
                        continue;
                    }
                    filename += Encoding.ASCII.GetString([b]);
                    StaticUtils.PrintLoader();
                }
                var bytes = cache[60..];
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

                if (filename.EndsWith('\\'))
                {
                    fsentries[filename + "A"] = byteoffset;
                }
                else
                {
                    fsentries[filename] = byteoffset;
                }
            }
            loc += 64;
        }

        return fsentries;
    }

    public static void ExtractBin(string source, string destination, bool extract_subfolder = true)
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
        var fs_entries = GetFsEntries(source);
        var write_to = "";
        using (Stream src = File.OpenRead(source))
        {
            var buffer = new byte[2048];
            var offset = 0;
            ulong finish = 0;
            var dnb = false;
            byte[] memory = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            var afiles = new List<string>();
            var lastfile = "";
            var content = new List<byte>();
            Console.Write("\r     Loading file to memory...".PadRight(Console.WindowWidth));
            while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                StaticUtils.PrintLoader();
                content.AddRange(buffer);
            }
            var c2 = content.ToArray<byte>();
            for (long loc = 0; loc < content.Count; loc+=2048)
            {
                byte[] entry = new byte[2048];
                Buffer.BlockCopy(c2, Convert.ToInt32(loc), entry, 0, 2048);
                if (loc % 16384 == 0)
                {
                    StaticUtils.PrintLoader();   
                }
                if (!dnb)
                {
                    if (lastfile.EndsWith("/A") && (extract_subfolder))
                    {
                        fs_entries.Remove(lastfile);
                        ExtractFolder(destination + "/" + lastfile, new FileInfo(destination + "/" + lastfile).DirectoryName ?? ".");
                        File.Delete(destination + "/" + lastfile);
                        lastfile = "";
                    }
                    foreach (KeyValuePair<string, long> kvp in fs_entries)
                    {
                        if ((kvp.Value == loc) && (!kvp.Key.EndsWith('\\')))
                        {
                            ulong min = (ulong)new FileInfo(source).Length;
                            foreach (KeyValuePair<string, long> kvp2 in fs_entries)
                            {
                                if ((kvp2.Value > kvp.Value) && ((ulong)kvp2.Value < min))
                                {
                                    min = (ulong)kvp2.Value;
                                }
                            }
                            var newDirName = new FileInfo(destination + "/" + kvp.Key).DirectoryName;
                            if ((newDirName != null) && !Directory.Exists(newDirName))
                            {
                                Console.Write(
                                    $"\r     Creating folder: {newDirName}");
                                Directory.CreateDirectory(newDirName);
                            }
                            finish = min;
                            lastfile = write_to;
                            if (!kvp.Key.EndsWith("\\A"))
                            {
                                afiles.Add(kvp.Key);
                                Console.Write(
                                    $"\r     Extracting {kvp.Key} ({StaticUtils.GetFilesizeString((long)finish - loc)})".PadRight(Console.WindowWidth));
                            }
                            else
                            {
                                Console.Write(
                                    $"\r     Extracting {kvp.Key[0..^1]} ({StaticUtils.GetFilesizeString((long)finish - loc)})".PadRight(Console.WindowWidth));
                            }
                            write_to = kvp.Key.Replace("\\", "/");
                            CheckMissingDirs(kvp.Key, destination);
                            dnb = true;
                        }
                    }
                }
                lastfile = write_to;
                if (dnb)
                {
                    using var stream = new FileStream(destination + "/" + write_to, FileMode.Append);
                    stream.Write(entry, 0, entry.Length);
                    StaticUtils.PrintLoader();
                }
                if ((dnb) && ((ulong)loc >= finish - 2048))
                {
                    dnb = false;
                }
            }
            if (lastfile.EndsWith("\\A"))
            {
                fs_entries.Remove(lastfile);
                ExtractFolder(destination + "/" + lastfile.Replace("\\", "/"), new FileInfo(destination + "/" + lastfile.Replace("\\", "/")).DirectoryName ?? ".");
                File.Delete(destination + "/" + lastfile.Replace("\\", "/"));
            }
        }
        Console.WriteLine($"\r   Files have been extracted to: {destination}".PadRight(Console.WindowWidth));

    }

    static int ExtractFolder(string source, string destination)
    {
        Console.Write($"\r     Extracting from subfolder at {new DirectoryInfo(source).Name}\\".PadRight(Console.WindowWidth));

        Console.Write("\r     Interpreting subfolder TOC data...".PadRight(Console.WindowWidth));
        var fs_entries = GetSubEntries(destination + "\\A");
        using Stream src = File.OpenRead(source);
        var buffer = new byte[1];
        var offset = 0;
        byte[] c2;
        var content = new List<byte>();
        Console.Write("\r     Loading subfolder to memory...".PadRight(Console.WindowWidth));
        StaticUtils.PrintLoader();
        while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
        {
            content.AddRange(buffer);
        }
        c2 = [.. content];
        var fs_values = new List<long>();
        var fs_keys = new List<string>();
        foreach (var kvp in fs_entries)
        {
            fs_values.Add(kvp.Value);
            fs_keys.Add(kvp.Key);
        }
        for (var i = 0; i < fs_entries.Count; i++)
        {
            var start = fs_values[i];
            long end = content.Count;
            if (i < fs_values.Count - 1)
            {
                end = fs_values[i + 1];
            }
            try
            {
                var entry = new byte[end - start];
                Console.Write($"\r     Extracting {fs_keys[i]} ({StaticUtils.GetFilesizeString(end - start)})".PadRight(Console.WindowWidth));
                try
                {
                    Buffer.BlockCopy(c2, Convert.ToInt32(start), entry, 0, (int)(end - start));
                    StaticUtils.PrintLoader();
                    File.WriteAllBytes(destination + "/" + fs_keys[i], entry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        return 0;
    }

    private static void CheckMissingDirs(string dirname, string target)
    {
        if (!dirname.Contains('\\')) return;
        Directory.CreateDirectory(target + "/" + dirname.Split("\\")[0]);
    }


    static Dictionary<string, long> GetSubEntries(string source)
    {
        var fsentries = new Dictionary<string, long>();
        using Stream src = File.OpenRead(source.Replace("\\", "/"));
        var buffer = new byte[64];
        var offset = 0;

        while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
        {
            var cache = buffer;
            var filename = cache[..60].Where(b => b != 0x00).Aggregate("", (current, b) => current + Encoding.ASCII.GetString([b]));

            if (filename == "*End Of Mem Data")
            {
                break;
            }
            var bytes = cache[60..];
            var byteoffset = (long)(BitConverter.ToInt32(bytes, 0));
            var original_filename = filename;
            var i = 1;
            while (fsentries.ContainsKey(filename))
            {
                filename = original_filename + "_" + i;
                i++;
            }
            fsentries[filename] = byteoffset;
        }

        return fsentries;
    }

}