using FlipnicLib;

namespace FlipnicFileTool.Tools;

public class IsoTools
{
    
    public IsoTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowIso:
                Console.Write(new IsoUdf(cfg.FileName).ToString(StaticUtils.SimpleOutput));
                break;
            case Enums.Modes.ExtractIso:
                ExtractIso(cfg.FileName, cfg.Output);
                break;
            case Enums.Modes.ReplaceIso:
                RepackIso(cfg.FileName, cfg.VFile, cfg.Output);
                break;
        }
    }

    private static void RepackIso(string filename, string vfsPath, string outputIso)
    {
        var iso = new IsoUdf(outputIso);
        new Thread( () =>
        {
            var result = iso.ReplaceFile(filename, outputIso, vfsPath);
            StaticUtils.LiveLoadStatus = result ? "Done!" : "Failed!";
        }).Start();
        Console.CursorVisible = false;
        while (StaticUtils.LiveLoadStatus != "Done!" && StaticUtils.LiveLoadStatus != "Failed!")
        {
            Thread.Sleep(100);
        }
        StaticUtils.LiveLoadStatus = "";
        Console.CursorVisible = true;
        if (StaticUtils.LiveLoadStatus == "Failed!")
        {
            Console.WriteLine("\rError: The file specified does not exist inside the .ISO file".PadRight(Console.WindowWidth, ' '));
            return;
        }
        Console.WriteLine($"\rThe contents of the following file have been modified: {outputIso}".PadRight(Console.WindowWidth, ' '));
    }

    private static void ExtractIso(string fileName, string outputDir)
    {
        var iso = new IsoUdf(fileName);
        Console.Write("Preparing to extract");
        if (Directory.Exists(outputDir))
        {
            Console.Write("\rThe specified directory already exists. Overwrite? [y/n] ");
            bool? overwrite = null;
            while (overwrite == null)
            {
                overwrite = Console.ReadKey().Key switch
                {
                    ConsoleKey.Y => true,
                    ConsoleKey.N => false,
                    _ => overwrite
                };
            }

            if (!(bool)overwrite)
            {
                Console.WriteLine("\rNo action was performed.".PadRight(Console.WindowWidth, ' '));
                return;
            }
        }
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        new Thread(() =>
        {
            iso.ExtractFiles(fileName, outputDir);
            StaticUtils.LiveLoadStatus = "Done!";
        }).Start();
        Console.CursorVisible = false;
        while (StaticUtils.LiveLoadStatus != "Done!")
        {
            Console.Write($"\r     {StaticUtils.LiveLoadStatus}".PadRight(Console.WindowWidth, ' '));
            StaticUtils.PrintLoader();
            Thread.Sleep(100);
        }
        Console.CursorVisible = true;
        Console.WriteLine($"\rFiles extracted to: {outputDir}".PadRight(Console.WindowWidth, ' '));
    }

}