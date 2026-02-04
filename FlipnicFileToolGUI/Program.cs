using Avalonia;
using FlipnicLib;
using System;
using System.Diagnostics;

namespace FlipnicFileToolGUI;

class Program
{
    public static readonly string AboutText =
        $"""
         Created by Markus Maal (a.k.a. Press any key to continue...)
         
         Powered by Avalonia UI using the SukiUI theme
         FlipnicLib version: {StaticUtils.DotFloatString(StaticUtils.LibVersion)}
         
         Disclaimer: {StaticUtils.DisclaimerText}
         """;
    
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
        catch (Exception e) when (!Debugger.IsAttached)
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

        if (OperatingSystem.IsLinux()) // restart not supported in Linux, just throw the damn exception
        {
            throw ex ?? new NullReferenceException();
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
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect().With(new MacOSPlatformOptions { DisableDefaultApplicationMenuItems = true })
            .WithInterFont()
            .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] }) // remove this one if you don't care about OpenGL support
            .LogToTrace();
}