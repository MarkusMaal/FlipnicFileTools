using System.Diagnostics;
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class ImageTools
{
    
    private string FileName { get; set; }
    private string Output { get; set; }
    
    private string MlbSect { get; set; }
    
    private string MagickPath { get; set; }

    public ImageTools(Config cfg)
    {
        FileName = cfg.FileName;
        Output = cfg.Output;
        MlbSect = cfg.MlbSect;
        MagickPath = cfg.MagickPath;
        
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowTim2:
                Console.WriteLine(
                    new Tim2(File.ReadAllBytes(FileName), FileName).ToString(StaticUtils.SimpleOutput));
                break;
            case Enums.Modes.ConvertTim2:
                ConvertTim2();
                break;
            case Enums.Modes.ShowMlb:
                Console.WriteLine(new FpnMlb(File.ReadAllBytes(FileName)).ToString(StaticUtils.SimpleOutput));
                break;
            case Enums.Modes.GenerateMockup:
                GenerateMockup();
                break;
        }
    }

    /// <summary>
    /// Convert TM2 texture file to a standard image file
    /// </summary>
    private void ConvertTim2()
    {
        var texture = new Tim2(File.ReadAllBytes(FileName), FileName);
        var fs = new FileStream(Output, FileMode.Create);
        texture.SavePng(fs);
    }

    /// <summary>
    /// Generate a menu mockup from the .MLB file
    /// </summary>
    private void GenerateMockup()
    {
        StaticUtils.GenerateEmptyPng(Output + "_", 640, StaticUtils.Pal ? 512 : 480);
        var root = new FileInfo(FileName).Directory?.FullName ?? ".";
        var magickCommand = $"\"{Output}_\" ";
        foreach (var sect in new FpnMlb(File.ReadAllBytes(FileName)).Sections
                     .Where(me => (MlbSect == "") || (me.Key == MlbSect)).SelectMany(me => me.Value))
        {
            try
            {
                var textureFile = sect.Texture.Split('\\')[^1].ToUpper();
                new Tim2(File.ReadAllBytes(Path.Combine(root, textureFile)),
                    Path.Combine(root, textureFile)).SavePng(
                    new FileStream(Path.Combine(root, textureFile.Replace(".TM2", ".TEMP.PNG")),
                        FileMode.Create));

                magickCommand +=
                    $" ( \"{Path.Combine(root, textureFile.Replace(".TM2", ".TEMP.PNG"))}\" ) -geometry +{sect.PosX}+{sect.PosY} -composite ";
            }
            catch
            {
                // ignored
            }
        }

        magickCommand += $" \"{Output}\"";
        Console.WriteLine($"Executing shell command: magick {magickCommand}");
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = MagickPath,
                Arguments = magickCommand.Replace("+−", "+"),
                UseShellExecute = true,
                CreateNoWindow = true,
            }
        };
        p.Start();
        p.WaitForExit();
        File.Delete(Output + "_");
        foreach (var f in new FileInfo(Output).Directory!.GetFiles())
        {
            if (f.Name.EndsWith(".TEMP"))
            {
                f.Delete();
            }
        }
    }
}