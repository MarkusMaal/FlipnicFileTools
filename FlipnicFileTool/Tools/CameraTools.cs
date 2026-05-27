using System.Xml.Linq;
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
                new FpnFpc(cfg.FileName).GenerateXml().Save(cfg.Output);
                SuccessMsg(cfg.Output);
                break;
            case Enums.Modes.ConvertFpc:
                var fpc = new FpnFpc(XDocument.Load(File.OpenRead(cfg.FileName)));
                File.WriteAllBytes(cfg.Output, fpc.GetBytes());
                SuccessMsg(cfg.Output);
                break;
        }
    }

    private static void SuccessMsg(string output)
    {
        StaticUtils.DecodeColors("~-ASuccess~--: File saved as " + output);
        Console.WriteLine();
    }
}