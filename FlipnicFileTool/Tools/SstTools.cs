using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class SstTools
{
    public SstTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.ListResources:
                Console.Write(new FpnSst(File.OpenRead(cfg.FileName)).GenerateMagicNumbers());
                break;
            case Enums.Modes.ShowSstToc:
                Console.Write(new FpnSst(File.OpenRead(cfg.FileName)).ListEntries());
                break;
            case Enums.Modes.ShowPseudoCode:
                Console.Write(new FpnSst(File.OpenRead(cfg.FileName)).GeneratePseudoCode());
                break;
            case Enums.Modes.ShowGimmick:
                new FpnSst(File.OpenRead(cfg.FileName)).ShowGimmick(cfg.SecondaryFileName);
                break;
            case Enums.Modes.ShowCameras:
                Console.Write(new FpnSst(File.OpenRead(cfg.FileName)).GetCamData(StaticUtils.SimpleOutput));
                break;
        }
    }
}