using System.Diagnostics;
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class ModelTools
{
    private string FileName { get; set; }
    private string Output { get; set; }
    
    public ModelTools(Config cfg)
    {
        FileName = cfg.FileName;
        Output = cfg.Output;

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowLp4: ShowLp4(); break;
            case Enums.Modes.ExportObj: ExportObj(); break;
            case Enums.Modes.ExportBbox: ExportBoxObj(); break;
            case Enums.Modes.ShowCol: Console.WriteLine(new FpnCol(FileName).ToString()); break;
            case Enums.Modes.ExportColObj: ExportColObj(FileName, cfg.SecondaryFileName, cfg.Output); break;
        }
    }

    /// <summary>
    /// Show metadata about the LP4 file
    /// </summary>
    private void ShowLp4()
    {
        var lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
        lp4.Read();
        Console.Write("\r".PadRight(Console.WindowWidth) + "\r");
        Console.WriteLine(lp4.ToString());
    }

    /// <summary>
    /// Generate Wavefront OBJ from a collision map
    /// </summary>
    /// <param name="input">Input .COL path</param>
    /// <param name="label">Specific mesh to export</param>
    /// <param name="output">Output .OBJ path</param>
    private static void ExportColObj(string input, string label, string output)
    {
        try
        {
            var objData = new FpnCol(input).GenerateObj(label);
            File.WriteAllText(output, objData);

            StaticUtils.DecodeColors($"~-ASuccess~--: File exported as {output}");
            Console.WriteLine();
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            StaticUtils.DecodeColors($"~-CError~--: {e.Message}");
        }
    }

    /// <summary>
    /// Convert the LP4 file to Wavefront OBJ
    /// </summary>
    private void ExportObj()
    {
        var lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
        lp4.Read();
        var id = 1;
        foreach (var model in lp4.Models)
        {
            lp4.SetSelectedModel(model);
            var ext = Path.GetExtension(Output);
            if (model.HasEmbeddedTexture)
            {
                lp4.Texture = model.GenerateDummyTexture();
            }
            StaticUtils.ExportObj(Output[..^ext.Length] + $".{model.Name}" + ext, lp4.GetVerticies(), lp4.Texture);
            id++;
        }

    }

    /// <summary>
    /// Convert the bounding box from the LP4 file to Wavefront OBJ
    /// </summary>
    private void ExportBoxObj()
    {
        var lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
        lp4.Read();
        StaticUtils.ExportObj(Output, lp4.GetBoundingBox(), null, true);
    }
}