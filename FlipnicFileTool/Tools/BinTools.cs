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
            case Enums.Modes.ExtractPak:
                new BinFile().ExtractPak(File.OpenRead(cfg.FileName), cfg.Output);
                break;
            case Enums.Modes.ListPak:
                new BinFile().ListPak(File.OpenRead(cfg.FileName));
                break;
            case Enums.Modes.ReplacePak:
                new BinFile().ListPak(File.Open(cfg.Output, FileMode.Open, FileAccess.ReadWrite), true, cfg.VFile, File.OpenRead(cfg.FileName));
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
            largeBuffer = vf.LargeBuffer;
        }
        
        if (!largeBuffer)
        {
            // When we get here, that means we are trying to replace a file inside a subdirectory (not alias, it's got to actually be inside the subdirectory container)
            var root = vFile.Split("\\")[0] + "\\";
            var rootOffset = -1L;
            var rootSize = -1L;
            // Identify the root directory this file is in
            foreach (var vf in binFiles)
            {
                if (vf.Path != "\\" + root) continue;
                rootOffset = vf.Offset;
                rootSize = vf.Length;
            }
            // Load the contents of the subdirectory to memory
            var ms =  new MemoryStream();
            using (var fs = File.OpenRead(outFile))
            {
                var buffer = new byte[2048];
                fs.Position = rootOffset;
                for (var i = 0; i < rootSize; i += 2048)
                {
                    fs.ReadExactly(buffer, 0, 2048);
                    ms.Write(buffer);
                }
            }

            // Replace the file inside a subdirectory
            // This will also perform shrink/expand operations within the context of the PAK
            ms.Position = 0;
            new BinFile().ListPak(ms, true, vFile.Split('\\')[1], File.OpenRead(filename));
            Console.WriteLine("Saving temporary file");
            
            // Creates a temporary PAK file of the modified subdirectory
            using (var ts = File.OpenWrite(root[..^1] + ".PAK"))
            {
                ms.Position = 0;
                for (var i = 0; i < ms.Length; i++)
                {
                    ts.WriteByte((byte)ms.ReadByte());
                }
            }
            Console.WriteLine("Replacing directory file");
            // Recursive call, we need to replace the subdirectory itself now
            ReplaceFile(root[..^1] + ".PAK", outFile, root);
            Console.WriteLine("Deleting temporary file");
            File.Delete(root[..^1] + ".PAK");
            return;
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
            RepackUtils.ResizeFile(vFile, (int)nSize, File.Open(outFile, FileMode.Open), binFiles);
        }
        Console.Write("\rRepacking...".PadRight(Console.WindowWidth, ' '));
        RepackUtils.RepackFileUnsafe(vfOffset, File.OpenRead(filename), outFile, vfSize, largeBuffer ? 2048 : 1);
        StaticUtils.DecodeColors("~-A\rSuccess~--: The file has been replaced!");
        Console.WriteLine();
    }
}