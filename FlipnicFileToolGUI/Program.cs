using Avalonia;
using FlipnicLib;
using System;
using System.Diagnostics;
using System.Globalization;

namespace FlipnicFileToolGUI;

internal abstract class Program
{
    
    private static string VersionString => StaticUtils.DotFloatString(StaticUtils.LibVersion) + (StaticUtils.IsBeta ? " BETA" : "");
    
    public static readonly string AboutText =
        $"""
         Created by Markus Maal (a.k.a. Press any key to continue...)
         
         Powered by Avalonia UI using the SukiUI theme
         FlipnicLib version: {VersionString}
         
         Disclaimer: {StaticUtils.DisclaimerText}
         """;

    public static bool GpuAccel;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            GpuAccel = args.Contains("--gpu");
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
            throw ex ?? new NullReferenceException("The exception is undefined");
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
            .With(GpuAccel ? new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] } : null)
            .LogToTrace();
}