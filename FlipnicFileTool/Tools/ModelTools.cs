using System.Diagnostics;
using System.Text.Json;
using FlipnicLib;
using FlipnicLib.Formats;
using FlipnicLib.Types;

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
            case Enums.Modes.ExportLp4Json: ExportLp4Json(cfg); break;
        }
    }

    /// <summary>
    /// Allows for parsing the entire content of a LP4 file and then serializing the parsed information as a JSON file
    /// </summary>
    /// <param name="cfg">Application configuration</param>
    private static void ExportLp4Json(
        Config cfg)
    {
        var fs = File.OpenRead(cfg.FileName);
        var lp4test = new Lp4(fs);
        fs.Close();
        var os = File.CreateText(cfg.Output);
        os.Write(JsonSerializer.Serialize(lp4test, Lp4TestGenerationContext.Default.Lp4));
        os.Close();
        StaticUtils.DecodeColors($"~-ASuccess~--: File exported as {cfg.Output}");
    }

    /// <summary>
    /// Show metadata about the LP4 file
    /// </summary>
    private void ShowLp4()
    {
        var lp4 = new Lp4(File.OpenRead(FileName));
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
        var lp4 = new Lp4(File.OpenRead(FileName));
        foreach (var chunk in (lp4.LayoutChunks ?? []).Where(chunk => chunk.LayoutChunkHeader.HasHitbox))
        {
            if (chunk.Model == null) continue;
            var model = chunk.Model;
            var ext = Path.GetExtension(Output);
            var textureFile = "";
            Tim2? texture = null;
            if (chunk.ModelVertexProperties.MaterialCount > 0)
            {
                textureFile = chunk.ModelVertexProperties.Materials[0].TextureFile;
            }

            List<float>? diffuse = null;

            if ((chunk.ModelProperties?[0].HasLightmap ?? false) && chunk.ModelProperties?[0].LightmapDataCount > 0)
            {
                diffuse = [];
                for (var i = 0; i < chunk.ModelProperties[0].LightmapDataCount; i++)
                {
                    diffuse.Add(chunk.ModelProperties[0].Lightmap[i].X);
                    diffuse.Add(chunk.ModelProperties[0].Lightmap[i].Y);
                    diffuse.Add(chunk.ModelProperties[0].Lightmap[i].Z);
                    diffuse.Add(chunk.ModelProperties[0].Lightmap[i].W);
                }
            }
            
            var tFileFull = Path.Join(new FileInfo(FileName).DirectoryName, textureFile.ToUpper());
            if (File.Exists(tFileFull))
            {
                texture = new Tim2(File.ReadAllBytes(tFileFull));
            }
            /*if (model.HasEmbeddedTexture)
            {
                lp4.Texture = model.GenerateDummyTexture();
            }*/
            try
            {
                StaticUtils.ExportObj(Output[..^ext.Length] + $".{chunk.Name}" + ext,
                    lp4.GetRawVertices((LayoutChunk.RawModel)model), texture, false, diffuse?.ToArray());
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
    }

    /// <summary>
    /// Convert the bounding box from the LP4 file to Wavefront OBJ
    /// </summary>
    private void ExportBoxObj()
    {
        var lp4 = new Lp4(File.OpenRead(FileName));
        StaticUtils.ExportObj(Output, lp4.GetBoundingBox(), null, true);
    }
}