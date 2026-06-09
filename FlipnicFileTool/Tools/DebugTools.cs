using System.Diagnostics;
using FlipnicLib;

namespace FlipnicFileTool.Tools;

public class DebugTools(Exception e)
{
    /// <summary>
    /// Display the fatal exception error screen
    /// </summary>
    /// <returns>255 when no exception should be thrown, -1 if the user requested an exception to be thrown</returns>
    public int Inspector()
    {
        var indentedTrace = "";
        if (e.StackTrace != null) indentedTrace = "  " + string.Join("\n     ", e.StackTrace.Split("\n"));
        Console.Clear();
        var betaStr = StaticUtils.IsBeta ? "Yes" : "No";
        StaticUtils.DecodeColors($"""
                                 ~-C
                                 Unhandled fatal exception~--
                                 This program has been halted due to a critical error. If this keeps happening, it may be a bug and should be reported to the developer!
                                 
                                 Context:
                                    Executable: {Process.GetCurrentProcess().ProcessName}
                                    CLI arguments: {Environment.CommandLine}
                                    Global variables:
                                       Simple output: {StaticUtils.SimpleOutput}
                                       Export envelopes: {StaticUtils.ExportEnvelopes}
                                       Is mode set: {StaticUtils.IsModeSet}
                                       Low memory: {StaticUtils.LowMem}
                                       Alt. SF2 method: {StaticUtils.AltSf2Method}
                                       Live load status: {StaticUtils.LiveLoadStatus}
                                       Message file: {StaticUtils.MsgFile}
                                       PAL: {StaticUtils.Pal}
                                       Window width: {StaticUtils.WindowWidth}
                                       Reverb strength: {StaticUtils.ReverbStrength}
                                       Force brute-force: {StaticUtils.ForceBruteForce}
                                 
                                 Environment:
                                    FlipnicLib version: {StaticUtils.DotFloatString(StaticUtils.LibVersion)}
                                    Beta version: {betaStr}
                                    Microsoft .NET version: {Environment.Version}
                                    Operating system: {Environment.OSVersion}
                                    Working directory: {Environment.CurrentDirectory}
                                    Memory allocation: {StaticUtils.GetFilesizeString(Environment.WorkingSet)}
                                    Page file: {StaticUtils.GetFilesizeString(Environment.SystemPageSize)}
                                    CPU time: {Environment.CpuUsage.TotalTime}
                                    System shutting down: {Environment.HasShutdownStarted}
                                 
                                 Technical info:
                                    {e.Message}
                                    {indentedTrace}
                                 """);
        Console.Write(
            "\n\nWe couldn't auto-detect a debugger being attached, but if you wish, you can still throw this exception.\n\nPressing Y will throw this exception to a JIT debugger\nPressing N will quit the application with an exit code\n\n[Y/N] ");
        while (true)
        {
            var key = Console.ReadKey();
            switch (key.Key)
            {
                case ConsoleKey.Y:
                    Console.WriteLine();
                    return -1;
                case ConsoleKey.N:
                    Console.WriteLine();
                    return 255;
                default:
                    continue;
            }
        }
    }
}