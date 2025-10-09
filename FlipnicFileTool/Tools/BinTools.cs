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
                new BinFile().ListBin(File.OpenRead(cfg.FileName));
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
        foreach (var vf in binFiles)
        {
            if (vf.Path != vFile && vf.Path[1..] != vFile) continue;
            vfOffset = vf.Offset;
            vfSize = vf.Length;
            largeBuffer = !vf.Path[1..].Contains('\\') || vf.Path[1..].EndsWith('\\');
        }

        if ((vfOffset == -1L) || (vfSize == -1L))
        {
            StaticUtils.DecodeColors("~-CError~--: The specified virtual file was not found");
            Console.WriteLine();
            return;
        }

        if (new FileInfo(filename).Length > vfSize)
        {
            StaticUtils.DecodeColors($"~-CError~--: The replacement file is too big! Must be {vfSize} bytes or less!");
            Console.WriteLine();
            return;
        }
        Console.Write("Repacking...");
        RepackUtils.RepackFileUnsafe(vfOffset, filename, outFile, vfSize, largeBuffer ? 2048 : 1);
        StaticUtils.DecodeColors("~-A\rSuccess~--: The file has been replaced!");
        Console.WriteLine();
    }
}