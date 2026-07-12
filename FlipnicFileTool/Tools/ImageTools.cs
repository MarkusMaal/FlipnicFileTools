using FlipnicLib;
using FlipnicLib.Formats;
using ImageMagick;
using Pixel = BigGustave.Pixel;

namespace FlipnicFileTool.Tools;

public class ImageTools
{
    
    private string FileName { get; set; }
    private string Output { get; set; }
    
    private string MlbSect { get; set; }

    public ImageTools(Config cfg)
    {
        FileName = cfg.FileName;
        Output = cfg.Output;
        MlbSect = cfg.MlbSect;
        
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
        var root = new FileInfo(FileName).Directory?.FullName ?? ".";
        using var baseImage = new MagickImage(StaticUtils.GenerateCheckerboardPng(640, StaticUtils.Pal ? 512 : 480,new Pixel(0,0,0, 0, false),new Pixel(0,0,0, 0 , false)));
        var mlb = new FpnMlb(File.ReadAllBytes(FileName));
        for (var depth = -32768; depth < 32768; depth++)
        {
            foreach (var item in mlb.Sections
                         .Where(me => MlbSect == "" || me.Key == MlbSect)
                         .SelectMany(me => me.Value.Select(v => new { me.Key, Value = v })))
            {
                var sect = item.Value;
                if (sect.Dipth != depth) continue;
                var textureFile = sect.Texture.Split('\\')[^1].ToUpper();
                var img = new MemoryStream();
                if (File.Exists(Path.Combine(root, textureFile)))
                {
                    var tim2 = new Tim2(File.ReadAllBytes(Path.Combine(root, textureFile)),
                        Path.Combine(root, textureFile));
                    foreach (var check in mlb.MenuColors)
                    {
                        if ((item.Key == check.SectionLabel) && (check.Index == sect.Index))
                        {
                            tim2.ReplaceColor(check.Color);
                        }
                    }
                    tim2.SavePng(img);
                }
                else
                {
                    img = (MemoryStream)StaticUtils.GenerateCheckerboardPng(sect.Width, sect.Height);
                }
                img.Position = 0;
                using var overlay =
                    new MagickImage(img);
                overlay.Resize(new MagickGeometry($"{sect.Width}x{sect.Height}!"));

                baseImage.Composite(overlay, sect.PosX, sect.PosY, CompositeOperator.Over);
            }
        }

        // Save result
        Console.WriteLine("Saving final PNG file");
        try
        {
            baseImage.Write(Output);
        }
        catch (UnauthorizedAccessException)
        {
            StaticUtils.DecodeColors("~-CError~--: Read-only file system");
            Console.WriteLine();
            return;
        }

        StaticUtils.DecodeColors($"~-ASuccess~--: File saved as {Output}");
        Console.WriteLine();
        
    }
}