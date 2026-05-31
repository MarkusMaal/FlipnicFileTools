using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Models;

namespace FlipnicFileToolGUI;

public class App : Application
{
    private static readonly SukiColorTheme AppTheme = new("AppTheme", Colors.BlueViolet, Colors.DeepPink);
    private static readonly SukiColorTheme SecTheme = new("Secondary theme", Colors.MidnightBlue, Colors.Purple);
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (Design.IsDesignMode) return;
        SukiTheme.GetInstance().AddColorTheme(AppTheme);
        SukiTheme.GetInstance().AddColorTheme(SecTheme);
        SukiTheme.GetInstance().ChangeBaseTheme(ThemeVariant.Dark);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (OperatingSystem.IsMacOS()) desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var mw = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.MainWindow = mw;
            if (desktop.Args?.Length > 0)
            {
                if (desktop.Args[0] == "-e") return;
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    if (!File.Exists(desktop.Args[0]))
                    {
                        Dispatcher.UIThread.Post(() => mw.ShowDialog("Error", "The specified file does not exist!", NotificationType.Error));
                        return;
                    } 
                    mw.FileName = desktop.Args[0];
                    Dispatcher.UIThread.Post(() => FileHelpers.LoadFromData(File.OpenRead(desktop.Args[0]), Path.GetExtension(desktop.Args[0])[1..], mw));
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
        var mw = new MainWindow
        {
            DataContext = new MainWindowViewModel(),
        };
        mw.Show();
        for (var i = 0; i < 2; i++)
        {
            SukiTheme.GetInstance().SwitchBaseTheme();
            mw.ApplyCustomTheme();
        }
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

    public static void Init(MainWindow mw)
    {
        if (!OperatingSystem.IsMacOS())
        {
            mw.RestartWglButton.IsVisible = !Program.GpuAccel;
        }
        mw.GlControl.IsVisible = Program.GpuAccel;
        mw.ModelInfoSection.IsVisible = Program.GpuAccel;
        mw.TpModelButton.IsVisible = Program.GpuAccel;
        mw.RotateModelCheck.IsVisible = Program.GpuAccel;
        if (Program.GpuAccel)
        {
            mw.FileMenu1.IsEnabled = false;
            mw.OptionMenu1.IsEnabled = false;
            mw.InfoMenu1.IsEnabled = false;
            mw.IsMenuVisible = false;
            mw.MainTabControl.SidebarToggleEnabled = false;
            mw.MainTabControl.HeaderMinHeight = 0;
            mw.ShowBottomBorder = false;
            mw.ModelTab.Icon = null;
            mw.IsTitleBarVisible = true;
            mw.TitleBarVisibilityOnFullScreen = SukiWindow.TitleBarVisibilityMode.Hidden;
            mw.WindowDecorations = WindowDecorations.Full;
            mw.MainTabControl.Margin = new Thickness(-48, 0, 0, 0);
        }
        if (Design.IsDesignMode)
        {
            mw.FileTypeLabel.Content = "Design mode";
            foreach (var tab in mw.MainTabControl.Items)
            {
                if (tab is SukiSideMenuItem ssmi)
                {
                    ssmi.IsVisible = true;
                }
            }
            return;
        }

        mw.InfoBox.Text = !OperatingSystem.IsLinux()
            ? """
              ---------------------------------
              Flipnic file tools
              ---------------------------------
              No file loaded, open a file by clicking File > Open
              or drag a file to this window.

              """
            : """
              ---------------------------------
              Flipnic file tools
              ---------------------------------
              No file loaded, open a file by clicking File > Open
              
              """;
        mw.ForceRefresh();
        var p = new Process();
        try
        {
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "imhex";
            p.StartInfo.Arguments = "--version";
            p.Start();
            p.WaitForExit();
            mw.OpenImHexMenuItem.IsVisible = p.ExitCode == 0;
        }
        catch
        {
            mw.OpenImHexMenuItem.IsVisible = false;
        }

        p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = OperatingSystem.IsWindows() ? "where" : "which";
        p.StartInfo.Arguments = "ffmpeg";
        p.Start();
        DetectFromOutput(p, mw.FFmpegBox , "FFmpeg", mw);
        mw.ReverbSlider.Value = StaticUtils.ReverbStrength;
        Preferences.LoadPreferences(mw);
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.Args?.Length == 0) return;
        if (MainWindow.ErrorDisplayed || desktop.Args?[0] != "-e") return;

        mw.InfoBox.Text = $"""
            ---------------------------------
            Flipnic file tools
            ---------------------------------
            The app was restarted because of a problem.
            If this keeps re-occuring, please report it to the developer!

            {desktop.Args[1]}
            {string.Join(" ", desktop.Args.Skip(3).ToArray())}
            """;
        MainWindow.ErrorDisplayed = true;
    }

    private static void DetectFromOutput(Process p, TextBox? textBox, string friendlyName, MainWindow mw)
    {
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            mw.InfoBox.Text += $"\n{friendlyName} is not installed";
            return;
        }
        if (output.Contains(';')) output = output.Split(';')[0];
        if (output.Contains('\n')) output = output.Replace("\r\n", "\n").Split('\n')[0];
        if (textBox != null) textBox.Text = output;
        mw.InfoBox.Text += $"\n{friendlyName} auto-detected at: {output}";
    }
}