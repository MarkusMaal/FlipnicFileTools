using FlipnicLib;

namespace FlipnicFileTool.Tools;

public class BinTools
{
    public BinTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ListBin:
                Console.Write(new BinFile().ListBin(File.OpenRead(cfg.FileName)));
                break;
            case Enums.Modes.ExtractBin:
                new BinFile().ExtractBin(File.OpenRead(cfg.FileName), cfg.Output);
                break;
            case Enums.Modes.ReplaceBin:
                ReplaceFile(cfg.FileName, cfg.Output, cfg.VFile);
                break;
        }
    }

    /// <summary>
    /// Safely replace an existing file inside the .BIN container
    /// </summary>
    /// <param name="filename">Path to replacement file</param>
    /// <param name="outFile">Path to the .BIN container</param>
    /// <param name="vFile">Name of the virtual file inside the .BIN container</param>
    private static void ReplaceFile(string filename, string outFile, string vFile)
    {
        var binFiles = new BinFile().GetListBin(File.OpenRead(outFile));
        var vfOffset = -1L;
        var vfSize = -1L;
        var largeBuffer = true;
        var rootDirOffset = 0L;
        var rootDirSize = 0L;
        var rootDirName = "";
        foreach (var vf in binFiles)
        {
            if (vf.Path != vFile && vf.Path[1..] != vFile) continue;
            vfOffset = vf.Offset;
            vfSize = vf.Length;
            largeBuffer = !vf.Path[1..].Contains('\\') || vf.Path[1..].EndsWith('\\');
        }

        if (!largeBuffer)
        {
            if (vFile.StartsWith('\\')) vFile = vFile[1..];
            var rootDir = binFiles.Where(bf => bf.Path == "\\" + vFile.Split('\\')[0] + "\\").ToArray()[0];
            rootDirName = rootDir.Path;
            rootDirOffset = rootDir.Offset;
            rootDirSize = rootDir.Length;
        }

        if ((vfOffset == -1L) || (vfSize == -1L))
        {
            StaticUtils.DecodeColors("~-CError~--: The specified virtual file was not found");
            Console.WriteLine();
            return;
        }

        if (new FileInfo(filename).Length > vfSize)
        {
            var nSize = new FileInfo(filename).Length;
            while ((nSize - vfSize) % 0x800 != 0)
            {
                if (!largeBuffer) break;
                nSize++;
            }
            StaticUtils.DecodeColors("~-EWarning~--: It seems like the input file is bigger than the original. This means, we are going to have to update file records and increase the size of the .BIN file. This operation is POTENTIALLY DANGEROUS and should only be done if you know exactly what you are doing!!! Are you sure you want to continue? ~-8[y/n]~--");
            while (true)
            {
                var ck = Console.ReadKey();
                var bn = false;
                switch (ck.Key)
                {
                    case ConsoleKey.Y:
                        bn = true;
                        break;
                    case ConsoleKey.N:
                        return;
                }

                if (bn) break;
            }

            Console.WriteLine();
            Console.Write("\rRebuilding .BIN file...");
            // The contents of this if-statement get executed when we need to resize a file
            // inside a subfolder (small buffer)
            if (!largeBuffer)
            {
                // Load the entire subfolder to memory
                var s2 = File.OpenRead(outFile);
                s2.Seek(rootDirOffset, SeekOrigin.Begin);
                var ms = new MemoryStream();
                for (var i = 0; i < rootDirSize; i++)
                {
                    ms.WriteByte((byte)s2.ReadByte());
                }

                s2.Close();

                // Resize subfolder entry and overwrite the contents
                var subF = new Subfolder(ms);
                var ns = new MemoryStream();
                var ns1 = subF.ResizeFile(vFile.Split('\\')[^1], (int)nSize, ns);
                var ns2 = subF.WriteFileUnsafe(vFile.Split('\\')[^1], File.ReadAllBytes(filename), ns1);
                
                // Ensure that the length can be addressed by 2048 bytes
                for (var i = 0; i < ns2.Length % 0x800; i++)
                {
                    ns2.WriteByte(0);
                }
                
                if (ns2.Length % 0x800 != 0) throw new FormatException("Stream length is not divisible by 2048");
                ns2.Position = 0;
                // Resize the subfolder container
                RepackUtils.ResizeFile(rootDirName, (int)ns2.Length, File.Open(outFile, FileMode.Open), binFiles);
                RepackUtils.RepackFileUnsafe(rootDirOffset, ns2, outFile, rootDirSize);
                ns2.Close();
                // Skip the normal repack process
                StaticUtils.DecodeColors("~-A\rSuccess~--: The file has been replaced!");
                Console.WriteLine();
                return;
            }
            RepackUtils.ResizeFile(vFile, (int)nSize, File.Open(outFile, FileMode.Open), binFiles);
        }
        Console.Write("\rRepacking...".PadRight(Console.WindowWidth, ' '));
        RepackUtils.RepackFileUnsafe(vfOffset, File.OpenRead(filename), outFile, vfSize, largeBuffer ? 2048 : 1);
        StaticUtils.DecodeColors("~-A\rSuccess~--: The file has been replaced!");
        Console.WriteLine();
    }
}