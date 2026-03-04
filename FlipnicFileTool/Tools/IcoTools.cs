using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class IcoTools
{
    public IcoTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowIco:
                ShowIco(cfg.FileName);
                break;
            case Enums.Modes.ConvertIcoTexture:
                ConvertIcoTexture(cfg.FileName, cfg.Output);
                break;
            case Enums.Modes.ConvertIcoObj:
                ConvertIcoObj(cfg.FileName, cfg.Output);
                break;
        }
    }
    
    /// <summary>
    /// Show metadata about the save icon
    /// </summary>
    /// <param name="fileName">Full path to the ICO file</param>
    private static void ShowIco(string fileName)
    {
        var ico = new SaveIcon(File.ReadAllBytes(fileName));
        ico.Read();
        Console.WriteLine(ico.ToString());
    }

    /// <summary>
    /// Extract textures from a save icon
    /// </summary>
    /// <param name="fileName">Full path to the ICO file</param>
    /// <param name="outFile">Full path to the output PNG file</param>
    private static void ConvertIcoTexture(string fileName, string outFile)
    {
        var ico = new SaveIcon(File.ReadAllBytes(fileName));
        ico.Read();
        ico.Texture?.SavePng(File.OpenWrite(outFile));
    }

    /// <summary>
    /// Convert save icon to Wavefront OBJ, also generates material files (NOTE: Works only with non-animated icons)
    /// </summary>
    /// <param name="fileName">Full path to the ICO file</param>
    /// <param name="outFile">Full path to the output OBJ file</param>
    private static void ConvertIcoObj(string fileName, string outFile)
    {
        var ico = new SaveIcon(File.ReadAllBytes(fileName));
        ico.Read();
        List<float> modelData = [];
        foreach (var vertex in ico.Vertices)
        {
            modelData.Add(vertex.TextureX / 4096f);
            modelData.Add(-vertex.TextureY / 4096f);
            modelData.Add(vertex.CoordX / 4096f);
            modelData.Add(-vertex.CoordY / 4096f);
            modelData.Add(-vertex.CoordZ / 4096f);
            modelData.Add(-vertex.NormalCoordX / 4096f);
            modelData.Add(vertex.NormalCoordY / 4096f);
            modelData.Add(-vertex.NormalCoordZ / 4096f);
        }
        StaticUtils.ExportObj(outFile, modelData.ToArray(), ico.Texture);
    }
}