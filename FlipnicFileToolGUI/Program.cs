using Avalonia;
using System;
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
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

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