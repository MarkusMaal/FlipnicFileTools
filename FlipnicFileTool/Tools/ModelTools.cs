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
        }
    }

    /// <summary>
    /// Show metadata about the LP4 file
    /// </summary>
    private void ShowLp4()
    {
        var lp4 = new Lp4(File.ReadAllBytes(FileName), FileName);
        lp4.Read();
        Console.WriteLine(lp4.ToString());
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
            StaticUtils.ExportObj(Output[..^ext.Length] + $".{model.Name}" + ext, lp4.GetVerticies(), lp4.Texture);
            id++;
        }

    }
}