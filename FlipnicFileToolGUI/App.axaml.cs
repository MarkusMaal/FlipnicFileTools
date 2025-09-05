using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using SukiUI;
using SukiUI.Models;

namespace FlipnicFileToolGUI;

public class App : Application
{
    public static SukiColorTheme AppTheme = new("AppTheme", Colors.DarkSlateBlue, Colors.DeepPink);
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SukiTheme.GetInstance().AddColorTheme(App.AppTheme);
        SukiTheme.GetInstance().ChangeBaseTheme(ThemeVariant.Dark);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void NativeMenuItem_OnClick(object? sender, EventArgs e)
    {
        Environment.Exit(0);
    }

    private void NewWindowMenuItem_OnClick(object? sender, EventArgs e)
    {
        new MainWindow().Show();
    }
}