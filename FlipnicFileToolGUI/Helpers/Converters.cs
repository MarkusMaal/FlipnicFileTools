using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FlipnicLib;
using FlipnicLib.Formats.Jam;

namespace FlipnicFileToolGUI.Helpers;

public static class Converters
{
    /// <summary>
    /// Converts .PSS file (semi-)directly to a .MOV file with audio tracks
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertMovAac(MainWindow mw)
    {
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        StaticUtils.Pal = mw.PalToggle.IsChecked ?? false;
        var outPut = (mw.FileBox.Text ?? "") + new FileInfo(mw.FileName).Name + ".MOV";
        var ffMpegPath = mw.FFmpegBox.Text ?? "";
        var originalFileName = mw.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Stage 1/4: Demuxing";
            new Pss(mw.FileName).ListPss(File.OpenRead(mw.FileName), true, new FileInfo(outPut).Directory!.FullName);
            StaticUtils.LiveLoadStatus = "Stage 2/4: Converting extracted IPU to MOV";
            var nf = Path.Combine(new FileInfo(outPut).Directory!.FullName, new FileInfo(mw.FileName).Name);
            Ipu.IpuConvert(nf + ".IPU", nf + ".TEMP.MOV", ffMpegPath);
            var exist = true;
            var streams = 0;
            StaticUtils.LiveLoadStatus = "Stage 3/4: Converting audio streams";
            while (exist)
            {
                if (File.Exists(
                        nf +
                        $".{++streams}.INT"))
                {
                    mw.FileName =
                        nf +
                        $".{streams}.INT";
                    StaticUtils.ConvertAudio(nf + $".{streams}.WAV", mw.FileName);
                    continue;
                }
                exist = false;
            }

            StaticUtils.LiveLoadStatus = "Stage 4/4: Generating final MOV file";
            var ffmpegCommand = $"-i \"{nf}.TEMP.MOV\" -i ";
            List<string> audioFiles = [];
            for (var i = 1; i < streams; i++)
            {
                audioFiles.Add($"\"{nf}.{i}.WAV\"");
            }
            ffmpegCommand += string.Join(" -i ", audioFiles);
            ffmpegCommand += " -map 0";
            for (var i = 1; i < streams; i++)
            {
                ffmpegCommand += $" -map {i}:a";
            }
            ffmpegCommand += $" -c:v copy -shortest \"{outPut}\"";
            StaticUtils.ProcessFFmpeg(ffMpegPath, ffmpegCommand);
            File.Delete(nf + ".TEMP.MOV");
            for (var i = 1; i <= streams; i++)
            {
                File.Delete(nf + $".{i}.WAV");
                File.Delete(nf + $".{i}.INT");
            }
            File.Delete(nf + ".IPU");
            Console.WriteLine($"\rFile saved as {outPut}");
            StaticUtils.LiveLoadStatus = "";
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
                mw.FileName = originalFileName;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => mw.LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    /// <summary>
    /// Separates audio/video streams from .PSS container
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void Demux(MainWindow mw)
    {
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        var outPut = mw.FileBox.Text ?? "";
        new Thread(() =>
        {
            new Pss(mw.FileName).ListPss(File.OpenRead(mw.FileName), true, outPut);
            StaticUtils.LiveLoadStatus = "";
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => mw.LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    /// <summary>
    /// Converts .IPU to .MOV without any audio tracks
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertMov(MainWindow mw)
    {
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        StaticUtils.Pal = mw.PalToggle.IsChecked ?? false;
        var outPut = (mw.FileBox.Text ?? "") + new FileInfo(mw.FileName).Name + ".MOV";
        var ffMpegPath = mw.FFmpegBox.Text ?? "";
        var originalFileName = mw.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Converting IPU to MOV";
            Ipu.IpuConvert(originalFileName, outPut, ffMpegPath);
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => mw.LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    /// <summary>
    /// Converts .HD/.BD voicebank to .SF2
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertSf2(MainWindow mw)
    {
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        StaticUtils.ExportVelocity = mw.VelocityToggle.IsChecked ?? false;
        StaticUtils.ExportEnvelopes = mw.EnvelopeToggle.IsChecked ?? false;
        var outFile = mw.FileBox.Text;
        var midiFile = mw.MidiBox.Text ?? "/no.where";
        var bdFile = mw.BdBox.Text ?? "/no.where";
        new Thread(() =>
        {
            Exception? error = null;
            try
            {
                StaticUtils.LiveLoadStatus = "Converting JAM to SF2";
                var extension = Path.GetExtension(mw.FileName);
                var fileName = new FileInfo(mw.FileName).Name.Replace(extension, "");
                Converter.InstrumentToSoundFont2(midiFile ?? "",
                    mw.FileName, bdFile ?? "", Path.Combine(outFile ?? "", fileName) + ".SF2");
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                error = ex;
            }

            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
                if (error is null)
                {
                    mw.ShowDialog("Flipnic file tools", "File converted successfully!", NotificationType.Success);
                    return;
                }
                mw.ShowDialog("Flipnic file tools", "Failed to convert file. Make sure that the correct BD file was selected.\n\n" + error.Message, NotificationType.Error);
            });
        }).Start();
    }
}