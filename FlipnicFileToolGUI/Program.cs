using Avalonia;
using System;
using System.Diagnostics;
using FlipnicLib;

namespace FlipnicFileToolGUI;

class Program
{
    public static string AboutText =
        $"Created by Markus Maal\n\nPowered by Avalonia UI using the SukiUI theme\nFlipnicLib version: {StaticUtils.DotFloatString(StaticUtils.LibVersion)}";

    public static bool MultiWindow = false;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Restart(e);
        }
    }
    
    
    private static void Restart(Exception? ex = null)
    {
        var exePath = Environment.ProcessPath;
        if (exePath is null)
        {
            Environment.Exit(255);
            return;
        }

        Process.Start(ex is not null
            ? new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                Arguments = "-e \"" + ex.Message.Replace("\"", "\\\"") + "\" + \"" +
                            (ex.StackTrace ?? "").Replace("\"", "\\\"") + "\""
            }
            : new ProcessStartInfo(exePath) { UseShellExecute = true });

        Environment.Exit(0);
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect().With(new MacOSPlatformOptions
            {
                DisableDefaultApplicationMenuItems = true
            })
            .WithInterFont()
            
            .LogToTrace();
}