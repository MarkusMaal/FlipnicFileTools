using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class CameraTools
{
    public CameraTools(Config cfg)
    {
        switch (cfg.Mode)
        {
            case Enums.Modes.ShowFpc:
                Console.Write(new FpnFpc(cfg.FileName).ToString(StaticUtils.SimpleOutput));
                break;
            case Enums.Modes.ConvertXml:
                new FpnFpc(cfg.FileName).GenerateXML().Save(cfg.Output);
                break;
        }
    }
}