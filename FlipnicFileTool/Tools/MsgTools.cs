using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileTool.Tools;

public class MsgTools
{
    public MsgTools(Config cfg)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (cfg.Mode)
        {
            case Enums.Modes.GenerateMsg:
                GenerateMsgFile(cfg.FileName, cfg.Output);
                break;
            case Enums.Modes.ShowMessages:
                ShowMessages(cfg.FileName);
                break;
        }
    }

    /// <summary>
    /// Display all messages inside a JA.MSG file
    /// </summary>
    /// <param name="fileName">Full path to the input file</param>
    private static void ShowMessages(string fileName)
    {
        Console.WriteLine(StaticUtils.SimpleOutput
            ? new FpnMsg(fileName).ToSimpleString()
            : new FpnMsg(fileName).ToString(StaticUtils.SimpleOutput));
    }

    /// <summary>
    /// Generate a JA.MSG file from a plain text file
    /// </summary>
    /// <param name="fileName">Path to input file (.TXT)</param>
    /// <param name="outFile">Path to output file (.MSG)</param>
    private static void GenerateMsgFile(string fileName, string outFile)
    {
        Console.WriteLine("Loading text file...");
        var lines = File.ReadAllLines(fileName);
        var msg = new FpnMsg
        {
            Messages = lines
        };
        Console.WriteLine("Saving message file...");
        File.WriteAllBytes(outFile, msg.GetData());
        Console.WriteLine($"File saved as {outFile}");
    }
}