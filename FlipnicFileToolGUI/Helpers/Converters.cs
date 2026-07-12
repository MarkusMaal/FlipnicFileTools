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
    /// Converts .PSS file (semi-)directly to a .MP4 file with audio tracks
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertMovAac(MainWindow mw)
    {
        mw.DockPanel1.IsVisible = false;
        StaticUtils.Pal = mw.PalToggle.IsChecked ?? false;
        var outPut = (mw.FileBox.Text ?? "") + new FileInfo(mw.FileName!).Name + ".MP4";
        var ffMpegPath = mw.FFmpegBox.Text ?? "";
        var originalFileName = mw.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Stage 1/4: Demuxing";
            new Pss(mw.FileName!).ListPss(File.OpenRead(mw.FileName!), true, new FileInfo(outPut).Directory!.FullName);
            StaticUtils.LiveLoadStatus = "Stage 2/4: Converting extracted IPU to M2V";
            var nf = Path.Combine(new FileInfo(outPut).Directory!.FullName, new FileInfo(mw.FileName!).Name);
            Ipu.IpuConvert(nf + ".IPU", nf + ".TEMP.M2V", ffMpegPath);
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

            StaticUtils.LiveLoadStatus = "Stage 4/4: Generating final MP4 file";
            var ffmpegCommand = $"-y -i \"{nf}.TEMP.M2V\" -i ";
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
            ffmpegCommand += $" -c:v libx264 -crf 3 -preset slow -shortest \"{outPut}\"";
            StaticUtils.ProcessFFmpeg(ffMpegPath, ffmpegCommand);
            File.Delete(nf + ".TEMP.M2V");
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
                mw.FileName = originalFileName;
            });
        }).Start();
    }

    /// <summary>
    /// Separates audio/video streams from .PSS container
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void Demux(MainWindow mw)
    {
        var outPut = mw.FileBox.Text ?? "";
        new Thread(() =>
        {
            new Pss(mw.FileName!).ListPss(File.OpenRead(mw.FileName!), true, outPut);
            StaticUtils.LiveLoadStatus = "";
        }).Start();
    }

    /// <summary>
    /// Converts .IPU to .M2V without any audio tracks
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertMov(MainWindow mw)
    {
        StaticUtils.Pal = mw.PalToggle.IsChecked ?? false;
        var outPut = (mw.FileBox.Text ?? "") + new FileInfo(mw.FileName!).Name + ".M2V";
        var ffMpegPath = mw.FFmpegBox.Text ?? "";
        var originalFileName = mw.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Converting IPU to M2V";
            Ipu.IpuConvert(originalFileName!, outPut, ffMpegPath);
            StaticUtils.LiveLoadStatus = "";
        }).Start();
    }

    /// <summary>
    /// Converts .HD/.BD voicebank to .SF2
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void ConvertSf2(MainWindow mw)
    {
        StaticUtils.ExportEnvelopes = mw.EnvelopeToggle.IsChecked ?? false;
        StaticUtils.ReverbStrength = (short)mw.ReverbSlider.Value;
        StaticUtils.AdsrMultipliers =
        [
            (int)mw.AttackSlider.Value * 10, (int)mw.DecaySlider.Value * 10, (int)mw.SustainSlider.Value * 10,
            (int)mw.ReleaseSlider.Value * 10
        ];
        var outFile = mw.FileBox.Text;
        var midiFile = mw.MidiBox.Text ?? "/no.where";
        var bdFile = mw.BdBox.Text ?? "/no.where";
        var createWav = mw.WavToggle.IsChecked ?? false;
        var fakeSustainR = mw.FakeSustainRateToggle.IsChecked ?? true;
        new Thread(() =>
        {
            Exception? error = null;
            try
            {
                StaticUtils.LiveLoadStatus = "Converting JAM to SF2";
                var extension = Path.GetExtension(mw.FileName);
                var fileName = new FileInfo(mw.FileName!).Name.Replace(extension!, "");
                Converter.InstrumentToSoundFont2(midiFile,
                    mw.FileName!, bdFile, Path.Combine(outFile ?? "", fileName) + ".SF2", createWav, fakeSustainR);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                error = ex;
            }

            StaticUtils.LiveLoadStatus = "";

            Dispatcher.UIThread.Post(() =>
            {
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