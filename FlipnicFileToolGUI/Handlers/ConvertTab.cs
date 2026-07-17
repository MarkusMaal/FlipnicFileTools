using System;
using System.IO;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FlipnicFileToolGUI.Helpers;
using SukiUI.Dialogs;

namespace FlipnicFileToolGUI.Handlers;

public abstract class ConvertTab
{
    public static void SliderUpdate(string? sourceName, double newValue, MainWindow mw)
    {
        switch (sourceName)
        {
            case "ReverbSlider":
                mw.ReverbStrengthLabel.Content = $"Reverb strength: {Math.Round(newValue / 10.0, 1)}%";
                break;
            case "AttackSlider":
                mw.AttackMultiplierLabel.Content = $"Attack strength: {Math.Round(newValue / 10.0, 1)}%";
                break;
            case "SustainSlider":
                mw.SustainMultiplierLabel.Content = $"Sustain strength: {Math.Round(newValue / 10.0, 1)}%";
                break;
            case "DecaySlider":
                mw.DecayMultiplierLabel.Content = $"Decay strength: {Math.Round(newValue / 10.0, 1)}%";
                break;
            case "ReleaseSlider":
                mw.ReleaseMultiplierLabel.Content = $"Release strength: {Math.Round(newValue / 10.0, 1)}%";
                break;
        }
    }

    public static void FFmpegBoxUpdate(MainWindow mw)
    {
        
        if (mw.FFmpegBox?.Text?.Length == 0) return;
        if (mw.FileBox?.Text?.Length == 0) return;
        var exist = new FileInfo(mw.FFmpegBox?.Text ?? "/no.where").Exists;
        var exist2 = new DirectoryInfo(mw.FileBox?.Text ?? "/no.where").Exists;
        mw.DemuxButton.IsEnabled = exist2;
        mw.ConvertMovAacButton.IsEnabled = exist && exist2;
        mw.ConvertMovButton.IsEnabled = exist && exist2;
        MidiBdChanged(mw);
    }

    public static void MidiBdChanged(MainWindow mw)
    {
        mw.ConvertSf2Button.IsEnabled = File.Exists(mw.BdBox.Text) && (!mw.MidiBrowserGrid.IsVisible || File.Exists(mw.MidiBox.Text)) && Directory.Exists(mw.FileBox.Text);
    }

    public static async void BrowseMidiBd(MainWindow mw, bool bdB)
    {
        try
        {
            var loadFiles = await mw.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Select " + (bdB ? "BD" : "MIDI") + " file",
                    FileTypeFilter = [bdB ? Filters.BdFile : Filters.MidiFile]
                });
            if (loadFiles.Count == 0) return;

            var fileName = Uri.UnescapeDataString(loadFiles[0].Path.AbsolutePath);
            if (bdB)
            {
                mw.BdBox.Text = fileName;
            }
            else
            {
                mw.MidiBox.Text = fileName;
            }
        }
        catch
        {
            // ignored
        }
    }

    public static void ConvertSf2Button(MainWindow mw)
    {
        if (Design.IsDesignMode) return;
        Converters.ConvertSf2(mw);
        new Thread(() =>
        {
            var visible = false;
            while (!visible)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    visible = mw.MainTabControl.IsVisible;
                });
            }
        }).Start();
    }

    public static void ConvertMovAacButton(MainWindow mw)
    {
        mw.DialogManager.CreateDialog()
            .WithTitle("Warning")
            .WithContent("This operation is lossy meaning the video quality may be reduced. For lossless conversion, please demux the file first and convert the streams separately.\n\nAre you sure you want to continue?")
            .WithActionButton("Yes", _ => { Converters.ConvertMovAac(mw); }, true)
            .WithActionButton("No", _ => { }, true)
            .OfType(NotificationType.Warning)
            .TryShow();
    }

    public static async void BrowseFolder(TextBox target, MainWindow mw)
    {
        try
        {
            var outputDir = await FileHelpers.SelectFolder(mw);
            if (outputDir == null) return;
            target.Text = outputDir;
        }
        catch (Exception e)
        {
            mw.ShowDialog("Flipnic file tools", "Error: " + e.Message, NotificationType.Error);
        }
    }

    public static async void BrowseFfmpeg(TextBox target, MainWindow mw)
    {
        try
        {
            var file = await FileHelpers.OpenFile(mw, [Filters.Executable], "Open FFmpeg binary");
            if (file == null) return;
            target.Text = file;
        }
        catch (Exception e)
        {
            mw.ShowDialog("Flipnic file tools", "Error: " + e.Message, NotificationType.Error);
        }
    }
}