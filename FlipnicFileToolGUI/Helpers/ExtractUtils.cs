using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using FlipnicLib;
using FlipnicLib.Formats.Vag;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.Helpers;

public class ExtractUtils
{
    /// <summary>
    /// Extract VAG sample from a BD file and convert it to WAV
    /// </summary>
    /// <param name="button">Control that invoked the method</param>
    /// <param name="mw">Main window instance</param>
    public static async void ExtractSample(Button button, MainWindow mw)
    {
        var filePath = Path.GetTempPath() + "/temp.wav";
        if ((button.Content?.ToString() ?? "") != "Play")
        {
            var file = await FileHelpers.SaveFile(mw, [Filters.WavFile]);
            if (file is null) return;
            filePath = file;
        }

        var ms = new MemoryStream();
        //var loopStart = mw.GetViewModel().Samples[mw.SamplesGrid.SelectedIndex].LoopStart;
        //var loopEnd = mw.GetViewModel().Samples[mw.SamplesGrid.SelectedIndex].LoopEnd;
        if (button.Content is "Extract sample" or "Play")
        {
            ms.Write(mw.GetViewModel().Samples![mw.SamplesGrid.SelectedIndex].Data.ToArray());
        }
        ms.Position = 0;
        var decodedData = SonyVag.Decode(ms.ToArray());
        ms = new MemoryStream();
        ms.Write(decodedData);
        // Mono, Signed 16-bit, 32000Hz
        var riff = new Riff(32000)
        {
            NumChannels = 1,
            BitsPerSample = 16,
            data = ms.ToArray(),
            
        };

        ms.Position = 0;
        ms.Write(riff.GetBytes());
        var fs = new FileStream(Uri.UnescapeDataString(filePath), FileMode.Create, FileAccess.Write);
        fs.Write(ms.ToArray(), 0, (int)ms.Length);
        fs.Close();
        JustPlay(mw);
    }
    
    /// <summary>
    /// Attempts to play a VAG/INT sample by first converting it to WAV into a temporary directory
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static void Play(MainWindow mw)
    {
        if (Design.IsDesignMode) return;
        var outPath = Path.GetTempPath() + "/temp.wav";
        mw.PlayButton.IsEnabled = false;
        mw.PlaybackStateLabel.Content = "Buffering";
        new Thread(() =>
        {
            StaticUtils.ConvertAudio(outPath, mw.FileName!, mw.FileName!.EndsWith("VAG"));
            Dispatcher.UIThread.Post(() => JustPlay(mw));
        }).Start();
    }

    /// <summary>
    /// Plays the temporary WAV without doing any conversions first
    /// </summary>
    /// <param name="mw">Main window instance</param>
    private static void JustPlay(MainWindow mw)
    {
        if (Design.IsDesignMode) return;
        var outPath = Path.GetTempPath() + "/temp.wav";
        var player = new NetCoreAudio.Player();
        player.Play(outPath);
        Dispatcher.UIThread.Post(() => {
            mw.PlaySampleButton.IsEnabled = false;
            mw.PlaybackStateLabel.Content = "Now playing";
        });
        player.PlaybackFinished += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                mw.PlaybackStateLabel.Content = "Cleaning";
            });
            while (FileHelpers.IsFileLocked(new FileInfo(outPath))) { Thread.Sleep(100); } // prevent race errors
            File.Delete(outPath);
            Dispatcher.UIThread.Post(() =>
            {
                mw.PlaybackStateLabel.Content = "Stopped";
                mw.PlayButton.IsEnabled = true;
                mw.PlaySampleButton.IsEnabled = true;
            });
        };
    }

    /// <summary>
    /// Extracts all files from a .BIN/.ISO container to a folder specified
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static async void ExtractAll(MainWindow mw)
    {
        var outputDir = await FileHelpers.SelectFolder(mw);
        if (outputDir is null) return;
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        mw.LoadProgress.IsIndeterminate = false;
        MainWindow.ProgressMax = 1;
        new Thread(() =>
        {
            while (MainWindow.ProgressMax != 0)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {

                    if (!mw.FileName!.ToUpper().EndsWith(".ISO"))
                    {
                        mw.LoadProgress.Maximum = MainWindow.ProgressMax;
                        mw.LoadProgress.Value = MainWindow.Progress;
                    }
                    else
                    {
                        mw.LoadStatus.Text = StaticUtils.LiveLoadStatus;
                    }
                });
            }
        }).Start();
        new Thread(() =>
        {
            if (mw.FileName!.ToUpper().EndsWith(".ISO"))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    mw.LoadProgress.IsIndeterminate = true;
                });
                new IsoUdf(mw.FileName).ExtractFiles(mw.FileName, outputDir);
                MainWindow.ProgressMax = 0;
            }
            else
            {
                foreach (var vf in mw.Fs!.FsEntries)
                {
                    if (vf.Path[1..].Contains('\\') && !Directory.Exists(outputDir + vf.Path.Split('\\')[1]))
                    {
                        Directory.CreateDirectory(outputDir + vf.Path.Split('\\')[1]);
                    }

                    if (vf.Path.EndsWith('\\')) continue;
                    Dispatcher.UIThread.Post(() =>
                    {
                        mw.LoadStatus.Text = $"Extracting {vf.Path} ({StaticUtils.GetFilesizeString(vf.Length)})";
                        MainWindow.Progress = 0;
                        MainWindow.ProgressMax = 1;
                    });
                    SaveFile(vf, outputDir + vf.Path.Replace("\\", "/"), mw);
                }
            }
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
                mw.LoadProgress.IsIndeterminate = true;
                MainWindow.ProgressMax = 0;
                MainWindow.Progress = 0;
            });
        }).Start();
    }


    /// <summary>
    /// Saves a file inside the .BIN container
    /// </summary>
    /// <param name="vf">VirtualFile object providing some information about the file</param>
    /// <param name="file">The file name as string</param>
    /// <param name="mw">Main window instance</param>
    internal static void SaveFile(VirtualFile vf, string file, MainWindow mw)
    {
        if (file.Contains('*')) return;
        var fs = new FileStream(mw.FileName!, FileMode.Open, FileAccess.Read);
        var os = new FileStream(file, FileMode.Create, FileAccess.Write);
        fs.Seek(vf.Offset, SeekOrigin.Begin);
        for (var i = 0; i < vf.Length / 2048; i += 1)
        {
            var buffer = new byte[2048];
            MainWindow.Progress = i;
            MainWindow.ProgressMax = (int)vf.Length / 2048;
            fs.ReadExactly(buffer);
            os.Write(buffer, 0, 2048);
        }

        try
        {
            var buffer2 = new byte[vf.Length % 2048];
            MainWindow.Progress = (int)vf.Length / 2048;
            MainWindow.ProgressMax = (int)vf.Length / 2048 + 1;
            fs.ReadExactly(buffer2);
            os.Write(buffer2, 0, (int)vf.Length % 2048);

        }
        catch (OverflowException)
        {
            // ignored
        }

        os.Close();
    }

    /// <summary>
    /// Extracts one file inside the .BIN container to a folder specified
    /// </summary>
    /// <param name="mw">Main window instance</param>
    public static async void Extract(MainWindow mw)
    {
        var file = await FileHelpers.SaveFile(mw, []);
        if (file == null) return;
        var vf = mw.FilesGrid.SelectedItem as VirtualFile;
        mw.DockPanel1.IsVisible = false;
        mw.Loader.IsVisible = true;
        mw.LoadProgress.IsIndeterminate = false;
        StaticUtils.LiveLoadStatus = $"Extracting {vf!.Path} ({StaticUtils.GetFilesizeString(vf.Length)})";
        MainWindow.ProgressMax = 1;
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (MainWindow.ProgressMax != 0)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    mw.LoadProgress.Maximum = MainWindow.ProgressMax;
                    mw.LoadProgress.Value = MainWindow.Progress;
                    mw.LoadStatus.Text = StaticUtils.LiveLoadStatus;
                });
            }
        }).Start();
        new Thread(() =>
        {
            SaveFile(vf, Uri.UnescapeDataString(file), mw);
            Dispatcher.UIThread.Post(() =>
            {
                mw.DockPanel1.IsVisible = true;
                mw.Loader.IsVisible = false;
                mw.LoadProgress.IsIndeterminate = true;
                MainWindow.ProgressMax = 0;
                MainWindow.Progress = 0;
            });
        }).Start();
    }
}