using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FlipnicLib;
using SukiUI;
using SukiUI.Models;

namespace FlipnicFileToolGUI;

public class App : Application
{
    public static SukiColorTheme AppTheme = new("AppTheme", Colors.BlueViolet, Colors.DeepPink);
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
            if (OperatingSystem.IsMacOS()) desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var mw = new MainWindow();
            desktop.MainWindow = mw;
            if (desktop.Args?.Length > 0)
            {
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    StaticUtils.FileName = desktop.Args[0];
                    Dispatcher.UIThread.Post(() => mw.LoadFromData(File.OpenRead(desktop.Args[0]), Path.GetExtension(desktop.Args[0])[1..]));
                }).Start();
            }
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

    private void AboutNativeMenu_OnClick(object? sender, EventArgs e)
    {
        // don't tell Apple
        var windows = ((IClassicDesktopStyleApplicationLifetime?)Current?.ApplicationLifetime)?.Windows;
        foreach (var window in windows ?? [])
        {
            if (!window.IsActive) continue;
            if (window is MainWindow mainWindow)
            {
                mainWindow.AboutClick(sender, null);
            }
        }
    }

    private void NativeMenu_OnOpening(object? sender, EventArgs e)
    {

        var windowCount = ((IClassicDesktopStyleApplicationLifetime?)Current?.ApplicationLifetime)?.Windows.Count;
        if (sender is not NativeMenu nativeMenu) return;
        foreach (var menuItem in nativeMenu.Items)
        {
            if (menuItem is not NativeMenuItem nativeMenuItem) continue;
            if (nativeMenuItem.Header == "About")
            {
                nativeMenuItem.IsEnabled = windowCount > 0;
            }
        }
    }
}