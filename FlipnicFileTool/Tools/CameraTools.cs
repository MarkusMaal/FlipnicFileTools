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
            case Enums.Modes.GenerateAnimation:
                var files = cfg.FileNameArr;
                if (files.Count < 2)
                {
                    StaticUtils.DecodeColors("~-CError~--: You must specify at least 2 input files!");
                    break;
                }
                InterpolateFrames(files[0], files[1], cfg.Output, cfg.Count);
                SuccessMsg(cfg.Output);
                break;
        }
    }

    private static void SuccessMsg(string output)
    {
        StaticUtils.DecodeColors("~-ASuccess~--: File saved as " + output);
        Console.WriteLine();
    }

    private static void InterpolateFrames(string input1, string input2, string output, int frameCount)
    {
        var fpc1 = new FpnFpc(input1);
        var fpc2 = new FpnFpc(input2);
        var initialOrigin = fpc1.GetOrigin();
        var initialTarget = fpc1.GetTarget();
        var initialFov = fpc1.GetFov();
        var finalOrigin = fpc2.GetOrigin();
        var finalTarget = fpc2.GetTarget();
        var finalFov = fpc2.GetFov();
        var stepOx = (finalOrigin[0] - initialOrigin[0]) / (frameCount - 2);
        var stepOy = (finalOrigin[1] - initialOrigin[1]) / (frameCount - 2);
        var stepOz = (finalOrigin[2] - initialOrigin[2]) / (frameCount - 2);
        var stepTx = (finalTarget[0] - initialTarget[0]) / (frameCount - 2);
        var stepTy = (finalTarget[1] - initialTarget[1]) / (frameCount - 2);
        var stepTz = (finalTarget[2] - initialTarget[2]) / (frameCount - 2);
        var stepFov = (finalFov - initialFov) / (frameCount - 2);
        fpc2.GenerateSequence(initialOrigin, initialTarget, initialFov, [stepOx, stepOy, stepOz, stepTx, stepTy, stepTz, stepFov], frameCount);
        File.WriteAllBytes(output, fpc2.GetBytes());
    }
}