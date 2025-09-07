using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FlipnicFileTool;
using FlipnicLib;
using FlipnicLib.Jam;
using FlipnicLib.Midi;
using FlipnicLib.Types;
using FlipnicLib.Vag;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using Syroot.BinaryData;

namespace FlipnicFileToolGUI;

public partial class MainWindow : SukiWindow
{
    private Dictionary<string, Gimmick[]>? Gimmicks { get; set; }
    
    public ObservableCollection<VirtualFile>? VirtualFiles { get; set; }
    
    public ObservableCollection<SampleColl> Samples { get; set; }
    
    List<MenuElementViewModel> _menu = [];

    public ObservableCollection<MenuElementViewModel> MenuElements { get; set; }
    
    private static int Progress { get; set; }
    private static int ProgressMax { get; set; }
    
    private const string FTypeFormat = "Type: {0}";

    private byte[] pcmData { get; set; }
    
    public ISukiDialogManager DialogManager = new SukiDialogManager();
    
    public MainWindow()
    {
        InitializeComponent();
        MenuElements = new ObservableCollection<MenuElementViewModel>(_menu);
        DataContext = this;
        ApplyCustomTheme();
        DialogHost.Manager = DialogManager;
        
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, WindowDropped);
        
        
    }

    private static void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
    
    private void ApplyCustomTheme()
    {
        SukiTheme.GetInstance().ChangeColorTheme(App.AppTheme);
    }

    private void PalMenuItem_OnClick(object? sender, RoutedEventArgs? e)
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
        ApplyCustomTheme();
    }
    
    private void OpenMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menu)
        {
            OpenMenuFromStr(menu.Header?.ToString() ?? "");
        }
    }

    private async void OpenFile(bool jaMsg = false)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = false,
            FileTypeFilter = jaMsg ? [Filters.FpnMsg] : [Filters.AllSupported, Filters.BinFile, Filters.FpnFpc, Filters.FpnSst, Filters.FpnLp4, Filters.FpnMlb,
                Filters.SonyPss, Filters.SonyTim2, Filters.MidiFile, Filters.HdFile, Filters.VsdFile, Filters.SvagFile]
        });

        if (files.Count <= 0) return;
        if (jaMsg)
        {
            StaticUtils.MsgFile = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
            return;
        }
        StaticUtils.FileName = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
        LoadFromData(new FileStream(Uri.UnescapeDataString(files[0].Path.AbsolutePath), FileMode.Open, FileAccess.Read), files[0].Path.AbsolutePath[^3..]);
        Title = "Flipnic file tool - " + new FileInfo(Uri.UnescapeDataString(files[0].Path.AbsolutePath)).Name;
    }

    private void WindowDropped(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        if (e.Data.GetFiles()?.First() == null) return;
        var fullPath = Uri.UnescapeDataString(e.Data.GetFiles()!.First().Path.AbsolutePath);
        StaticUtils.FileName = fullPath;
        LoadFromData(new FileStream(fullPath, FileMode.Open, FileAccess.Read), fullPath[^3..]);
        Title = "Flipnic file tool - " + new FileInfo(fullPath).Name;
    }

    private void LoadAsString(object? sender, string type)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InfoBox.Text = sender?.ToString() ?? "";
            InfoTab.IsVisible = true;
            FileTypeLabel.Content = string.Format(FTypeFormat, type);
        });
    }

    internal void LoadFromData(Stream ds, string ext)
    {
        FileTypeLabel.Content = "Please wait...";
        foreach (var t in MainTabControl.Items)
        {
            ((SukiSideMenuItem)t!)!.IsVisible = false;
        }

        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        
        new Thread(() =>
        {
            switch (ext)
            {
                case "TM2":
                    var data = new byte[ds.Length];
                    ds.ReadExactly(data);
                    ds.Position = 0;
                    var img = new Tim2(data);
                    var bt = new BitmapTools { Image = img };
                    LoadAsString(img, "PlayStation 2 texture file");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ImagePreviewTab.IsVisible = true;
                        PreviewImage.Source = bt.ToBitmap();
                    });
                    break;
                case "MID":
                    var midi = new Midi();
                    midi.Read(ds);
                    LoadAsString(midi, "General MIDI");
                    break;
                case "BD": 
                case ".BD":
                    Samples = [];
                    var s = new Samples(ds);
                    var samples = new List<SampleColl>();
                    var offset = 0;
                    for (var i = 0; i < s.RawSamples.Count; i++)
                    {
                        samples.Add(new SampleColl
                        {
                            Data = s.RawSamples[i],
                            Id = i,
                            Offset = (int)offset,
                            LoopStart = s.LoopStarts[i],
                            LoopEnd = s.LoopEnds[i],
                        });
                        offset += s.Lengths[i];
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        Samples = new ObservableCollection<SampleColl>(samples);
                        BdSampleTab.IsVisible = true;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "JAM body");
                    });
                    break;
                case "HD":
                case ".HD":
                    var jh = new JamHeader();
                    jh.Read(new BinaryStream(ds));
                    LoadAsString(jh, "JAM header");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ConvertTab.IsVisible = true;
                        FfmpegBrowserGrid.IsVisible = false;
                        PalToggle.IsVisible = false;
                        ConvertSf2Button.IsVisible = true;
                        ConvertMovAacButton.IsVisible = false;
                        ConvertMovButton.IsVisible = false;
                        DemuxButton.IsVisible = false;
                    });
                    break;
                case "VSD":
                    var vsd = new FpnVsd(ds);
                    LoadAsString(vsd, "Vibration Strength Data");
                    break;
                case "INT":
                case "VAG":
                    var va = new byte[ds.Length];
                    ds.ReadExactly(va);
                    pcmData = SonyVag.Decode(va);
                    Dispatcher.UIThread.Post(() =>
                    {
                        SoundPlayerTab.IsVisible = true;
                        AudioFilename.Content = "Filename: " + Path.GetFileName(StaticUtils.FileName);
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Compressed Sony ADPCM Audio " + (StaticUtils.FileName.EndsWith("INT") ? "(Stereo)" : "(Mono)"));
                    });
                    break;
                case "MLB":
                    var mlbDa = new byte[ds.Length];
                    ds.ReadExactly(mlbDa);
                    var mlb = new FpnMlb(mlbDa);
                    StaticUtils.LiveLoadStatus = "Generating menu...";
                    _menu.Clear();
                    var menuIndex = 0;
                    Dispatcher.UIThread.Post(() => LoadProgress.IsIndeterminate = false);
                    Dispatcher.UIThread.Post(() => LoadProgress.Maximum = mlb.Sections.Count);
                    foreach (var sect in mlb.Sections)
                    {
                        try
                        {
                            _menu.AddRange(from ima in sect.Value
                                let p =
                                    Path.Combine(Path.GetDirectoryName(StaticUtils.FileName) ?? string.Empty,
                                        ima.Texture.Split('\\')[^1])
                                let bmp = new BitmapTools { Image = new Tim2(File.ReadAllBytes(p)), }.ToBitmap()
                                select new MenuElementViewModel
                                    { Layer = sect.Key, MenuElement = ima, ImageSource = bmp });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("ERROR: " + ex.Message);
                        }

                        Dispatcher.UIThread.Post(() => LoadProgress.Value = ++menuIndex);
                    }
                    Dispatcher.UIThread.Post(() => LoadProgress.IsIndeterminate = true);
                    StaticUtils.LiveLoadStatus = "Please wait...";
                    

                    Dispatcher.UIThread.Post(() =>
                    {
                        MenuMockupTab.IsVisible = true;
                        MenuMockup.MenuElementSource = new ObservableCollection<MenuElementViewModel>(_menu);
                    });
                    LoadAsString(mlb, "Menu layout file");
                    break;
                case "LP4":
                    var lp4Da =  new byte[ds.Length];
                    ds.ReadExactly(lp4Da);
                    var lp4 = new Lp4(lp4Da);
                    LoadAsString(lp4, "Flipnic resource file");
                    Dispatcher.UIThread.Post(() => ModelTab.IsVisible = true);
                    break;
                case "IPU":
                    var ipu = Ipu.GetInfoAsString(ds);
                    LoadAsString(ipu, "IPU video stream");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ConvertTab.IsVisible = true;
                        ConvertMovButton.IsVisible = true;
                        ConvertMovAacButton.IsVisible = false;
                        FfmpegBrowserGrid.IsVisible = true;
                        PalToggle.IsVisible = true;
                        ConvertSf2Button.IsVisible = false;
                        DemuxButton.IsVisible = false;
                    });
                    break;
                case "LAY":
                    var da = new byte[ds.Length];
                    ds.ReadExactly(da);
                    var lay = new FpnLay(da);
                    LoadAsString(lay, "Stage layout file");
                    break;
                case "MSG":
                    var msg = new FpnMsg(ds);
                    LoadAsString(msg, "Message table");
                    break;
                case "FPC":
                    var fpc = new FpnFpc(ds);
                    LoadAsString(fpc, "Camera sequence");
                    break;
                case "SST":
                    var sst = new FpnSst(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        InfoBox.Text = $"Entries\n{sst.ListEntries()}\n\nResources\n{sst.GenerateMagicNumbers()}";
                        Gimmicks = sst.GetGimmicks();
                        StageGimmickTab.IsVisible = Gimmicks?.Count > 0;
                        GimmickCombobox.Items.Clear();
                        foreach (var key in Gimmicks?.Keys.ToArray() ?? [])
                        {
                            GimmickCombobox.Items.Add(key);
                        }
                        PseudoCodeTab.IsVisible = sst.TableOfContents.ContainsKey("EVENT");
                        if (PseudoCodeTab.IsVisible)
                        {
                            EventBox.Text = sst.GeneratePseudoCode();
                        }

                        GimmickCombobox.SelectedIndex = 0;
                        InfoTab.IsVisible = true;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Stage information file");
                    });
                    break;
                case "BIN":
                    BinFile.FsEntries.Clear();
                    BinFile.ListBin(ds);
                    
                    var fsEntries = BinFile.FsEntries.ToList();
                    Dispatcher.UIThread.Post(() =>
                    {
                        VirtualFiles = new ObservableCollection<VirtualFile>(fsEntries);
                        FileListTab.IsVisible = true;
                        DataContext = this;
                        FilesGrid.ItemsSource = VirtualFiles;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Blob file");
                    });
                    break;
                case "PSS":
                    var pssInfo = Pss.ListPss(ds);
                    Dispatcher.UIThread.Post(() =>
                    {
                        InfoBox.Text = pssInfo;
                        ConvertTab.IsVisible = true;
                        InfoTab.IsVisible = true;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Interleaved video/audio streams");
                        ConvertMovButton.IsVisible = false;
                        ConvertMovAacButton.IsVisible = true;
                        DemuxButton.IsVisible = true;
                        FfmpegBrowserGrid.IsVisible = true;
                        PalToggle.IsVisible = true;
                        ConvertSf2Button.IsVisible = false;
                    });
                    break;
                default:
                    Dispatcher.UIThread.Post(() =>
                    {
                        InfoBox.Text = "Unrecognized file type";
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Unknown");
                        InfoTab.IsVisible = true;
                    });
                    break;
            }
            ds.Close();

            StaticUtils.LiveLoadStatus = "";
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                // switch to first visible tab
                MainTabControl.UnselectAll();
                foreach (SukiSideMenuItem? sSmi in MainTabControl.Items)
                {
                    if (sSmi is not { IsVisible: true }) continue;
                    sSmi.IsSelected = true;
                    break;
                }
            });
        }).Start();
        // display loading screen if applicable
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    private void Play()
    {
        var outPath = Path.GetTempPath() + "/temp.wav";
        StaticUtils.ConvertAudio(outPath,  StaticUtils.FileName.EndsWith("VAG"));
        JustPlay();
    }

    private void JustPlay()
    {
        var outPath = Path.GetTempPath() + "/temp.wav";
        var player = new NetCoreAudio.Player();
        PlayButton.IsEnabled = false;
        PlaySampleButton.IsEnabled = false;
        player.Play(outPath);
        PlaybackStateLabel.Content = "Now playing";
        player.PlaybackFinished += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PlaybackStateLabel.Content = "Stopped";
                PlayButton.IsEnabled = true;
                PlaySampleButton.IsEnabled = true;
            });
            File.Delete(outPath);
        };
    }
    
    private void GimmickCombobox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
       var val = ((ComboBox)sender!).SelectedValue?.ToString();
       var gimmickList = Gimmicks?[val!];
       string[] colHeaders = ["Label", "Type", "Button", "Sound effect", "Flip. strength", "Knockback", "Bounciness"];
       List<string[]> rows = [];
       if (gimmickList == null) return;
       rows.AddRange(gimmickList.Select(entry => (string[])
       [
           entry.Label, entry.Type.ToString(), entry.Button.ToString(), entry.SoundEffect.ToString(),
           StaticUtils.DotFloatString(entry.FlipperStrength), StaticUtils.DotFloatString(entry.Knockback),
           StaticUtils.DotFloatString(entry.Bounciness)
       ]));
       GimmickBox.Text = StaticUtils.GenerateTable(colHeaders, rows,
           rows.Select(row => row[0].Length).Prepend(15).Max());
    }

    private void OpenMenuFromStr(string header)
    {
        switch (header)
        {
            case "Open":
                OpenFile();
                break;
            case "Import JA.MSG":
                OpenFile(true);
                break;
        }
    }

    private void OpenNativeMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (sender is NativeMenuItem menu)
        {
            OpenMenuFromStr(menu.Header ?? "");
        }
    }
    
    private void ExitMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        InfoBox.Text = """
                       ---------------------------------
                       Flipnic file tools
                       ---------------------------------
                       No file loaded, open a file by clicking File > Open
                       or drag a file to this window.

                       """;
        
        var p = new Process();
        
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = OperatingSystem.IsWindows() ? "where" : "which";
        p.StartInfo.Arguments = "ffmpeg";
        p.Start();
        DetectFromOutput(p, FFmpegBox , "FFmpeg");
    }

    private void DetectFromOutput(Process p, TextBox? textBox, string friendlyName)
    {
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            InfoBox.Text += $"\n{friendlyName} is not installed";
            return;
        }
        if (output.Contains(';')) output = output.Split(';')[0];
        if (output.Contains('\n')) output = output.Replace("\r\n", "\n").Split('\n')[0];
        if (textBox != null) textBox.Text = output;
        InfoBox.Text += $"\n{friendlyName} auto-detected at: {output}";
    }
    
    private void NewWindowMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        OpenButton.IsEnabled = ((DataGrid)sender!).SelectedIndex != -1;
        ExtractButton.IsEnabled = ((DataGrid)sender!).SelectedIndex != -1;
    }

    private void OpenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vf = FilesGrid.SelectedItem as VirtualFile;
        var myTitle = Title;
        var mw = new MainWindow()
        {
            Title = myTitle + vf!.Path
        };
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Reading data...";
            var fs = new FileStream(StaticUtils.FileName, FileMode.Open, FileAccess.Read);
            var ms = new MemoryStream();
            var buffer = new byte[vf!.Length];
            fs.Seek(vf.Offset, SeekOrigin.Begin);
            fs.ReadExactly(buffer);
            fs.Close();
            ms.Write(buffer, 0,(int)vf.Length);
            ms.Position = 0;
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                mw.Show();
                mw.LoadFromData(ms, vf.Path[^3..]);
            });
        }).Start();
    }

    private async void ExtractButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
        });

        if (file is null) return;
        var vf = FilesGrid.SelectedItem as VirtualFile;
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        LoadProgress.IsIndeterminate = false;
        StaticUtils.LiveLoadStatus = $"Extracting {vf!.Path} ({StaticUtils.GetFilesizeString(vf.Length)})";
        ProgressMax = 1;
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (ProgressMax != 0)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    LoadProgress.Maximum = ProgressMax;
                    LoadProgress.Value = Progress;
                    LoadStatus.Text = StaticUtils.LiveLoadStatus;
                });
            }
        }).Start();
        new Thread(() =>
        {
            SaveFile(vf!, Uri.UnescapeDataString(file.Path.AbsolutePath));
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                LoadProgress.IsIndeterminate = true;
                ProgressMax = 0;
                Progress = 0;
            });
        }).Start();
    }

    private static void SaveFile(VirtualFile vf, string file)
    {
        if (file.Contains('*')) return;
        var fs = new FileStream(StaticUtils.FileName, FileMode.Open, FileAccess.Read);
        var os = new FileStream(file, FileMode.Create, FileAccess.Write);
        fs.Seek(vf.Offset, SeekOrigin.Begin);
        for (var i = 0; i < vf.Length / 2048; i += 1)
        {
            var buffer = new byte[2048];
            Progress = i;
            ProgressMax = (int)vf.Length / 2048;
            fs.ReadExactly(buffer);
            os.Write(buffer, 0, 2048);
        }

        try
        {
            var buffer2 = new byte[vf.Length % 2048];
            Progress = (int)vf.Length / 2048;
            ProgressMax = (int)vf.Length / 2048 + 1;
            fs.ReadExactly(buffer2);
            os.Write(buffer2, 0, (int)vf.Length % 2048);

        }
        catch (OverflowException)
        {
            // ignored
        }

        os.Close();
    }

    private async void ExtractAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var storageFiles = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select folder"
            });
        if (storageFiles.Count == 0) return;
        var outputDir = Uri.UnescapeDataString(storageFiles[0].Path.AbsolutePath);
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        LoadProgress.IsIndeterminate = false;
        ProgressMax = 1;
        new Thread(() =>
        {
            while (ProgressMax != 0)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    LoadProgress.Maximum = ProgressMax;
                    LoadProgress.Value = Progress;
                });
            }
        }).Start();
        new Thread(() =>
        {
            foreach (var vf in BinFile.FsEntries)
            {
                if (vf.Path[1..].Contains('\\') && !Directory.Exists(outputDir + vf.Path.Split('\\')[1]))
                {
                    Directory.CreateDirectory(outputDir + vf.Path.Split('\\')[1]);
                }

                if (vf.Path.EndsWith('\\')) continue;
                Dispatcher.UIThread.Post(() =>
                {
                    LoadStatus.Text = $"Extracting {vf.Path} ({StaticUtils.GetFilesizeString(vf.Length)})";
                    Progress = 0;
                    ProgressMax = 1;
                });
                SaveFile(vf, outputDir + vf.Path.Replace("\\", "/"));
            }
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                LoadProgress.IsIndeterminate = true;
                ProgressMax = 0;
                Progress = 0;
            });
        }).Start();
    }

    private void FFmpegBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (FFmpegBox?.Text?.Length == 0) return;
        var exist = new FileInfo(FFmpegBox?.Text ?? "/no.where").Exists;
        var exist2 = new DirectoryInfo(FileBox?.Text ?? "/no.where").Exists;
        DemuxButton.IsEnabled = exist2;
        ConvertMovAacButton.IsEnabled = exist && exist2;
        ConvertMovButton.IsEnabled = exist && exist2;
    }

    private async void BrowseButtonFfmpeg_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open FFmpeg binary",
            AllowMultiple = false,
            FileTypeFilter = [Filters.Executable]
        });
        
        if (files.Count <= 0) return;
        
        FFmpegBox.Text = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
    }

    private async void BrowsButtonPath_OnClick(object? sender, RoutedEventArgs e)
    {
        var storageFiles = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select folder"
            });
        if (storageFiles.Count == 0) return;
        var outputDir = Uri.UnescapeDataString(storageFiles[0].Path.AbsolutePath);
        
        FileBox.Text = outputDir;
    }

    private void DemuxButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        var outPut = FileBox.Text ?? "";
        new Thread(() =>
        {
            Pss.ListPss(File.OpenRead(StaticUtils.FileName), true, outPut);
            StaticUtils.LiveLoadStatus = "";
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    private void ConvertMovAacButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        var outPut = (FileBox.Text ?? "") + new FileInfo(StaticUtils.FileName).Name + ".MOV";
        var ffMpegPath = FFmpegBox.Text ?? "";
        var originalFileName = StaticUtils.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Stage 1/4: Demuxing";
            Pss.ListPss(File.OpenRead(StaticUtils.FileName), true, new FileInfo(outPut).Directory!.FullName);
            StaticUtils.LiveLoadStatus = "Stage 2/4: Converting extracted IPU to MOV";
            var nf = Path.Combine(new FileInfo(outPut).Directory!.FullName, new FileInfo(StaticUtils.FileName).Name);
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
                    StaticUtils.FileName =
                        nf +
                        $".{streams}.INT";
                    StaticUtils.ConvertAudio(nf + $".{streams}.WAV");
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
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                StaticUtils.FileName = originalFileName;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    private void PalToggle_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is null) return;
        StaticUtils.Pal = ((CheckBox)sender).IsChecked ?? false;
    }

    private void CloseNativeMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void CloseMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void AboutClick(object? sender, RoutedEventArgs? e)
    {
        ShowDialog("Flipnic file tools",
            Program.AboutText,
            NotificationType.Information);
    }

    public void ShowDialog(string title, string content, NotificationType type)
    {
        DialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(content)
            .WithActionButton("OK", _ => {}, true)
            .OfType(type)
            .TryShow();
    }

    private async void SaveImgAsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [Filters.PngFile]
        });

        if (file is null) return;
        
        ((Bitmap?)PreviewImage.Source)?.Save(Uri.UnescapeDataString(file.Path.AbsolutePath));
        ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
    }

    private void ToggleDarkNative(object? sender, EventArgs e)
    {
        PalMenuItem_OnClick(sender, null);
    }

    private void NativeAboutClick(object? sender, EventArgs e)
    {
        AboutClick(sender, null);
    }

    private void PlayButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Play();
    }

    private async void SaveSoundAsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [Filters.WavFile]
        });

        if (file is null) return;
        var outPath = Uri.UnescapeDataString(file.Path.AbsolutePath);
        StaticUtils.ConvertAudio(outPath, StaticUtils.FileName.EndsWith("VAG"));
        ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
    }

    private void ConvertMovButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        var outPut = (FileBox.Text ?? "") + new FileInfo(StaticUtils.FileName).Name + ".MOV";
        var ffMpegPath = FFmpegBox.Text ?? "";
        var originalFileName = StaticUtils.FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Converting IPU to MOV";
            Ipu.IpuConvert(originalFileName, outPut, ffMpegPath);
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                StaticUtils.FileName = originalFileName;
            });
        }).Start();
        new Thread(() =>
        {
            Thread.Sleep(100);
            while (true)
            {
                if (StaticUtils.LiveLoadStatus == "") break;
                Dispatcher.UIThread.Post(() => LoadStatus.Text = StaticUtils.LiveLoadStatus);
                Thread.Sleep(100);
            }
        }).Start();
    }

    private void ConvertSf2Button_OnClick(object? sender, RoutedEventArgs e)
    {
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        var outFile = FileBox.Text;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Converting JAM to SF2";
            var fileDirectory = new FileInfo(StaticUtils.FileName).Directory?.FullName ?? "";
            var extension = Path.GetExtension(StaticUtils.FileName);
            var fileName = new FileInfo(StaticUtils.FileName).Name.Replace(extension, "");
            Converter.InstrumentToSoundFont2(Path.Combine(fileDirectory, fileName) + ".MID",
                StaticUtils.FileName, Path.Combine(fileDirectory, fileName) + ".BD", Path.Combine(outFile ?? "", fileName) + ".SF2");
            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false; 
            });
        }).Start();
        ShowDialog("Flipnic file tools", "File converted successfully!\n\nNote: JAM to SF2 conversion is kind of borked currently...", NotificationType.Success);
    }

    private async void ExtractSampleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var filePath = Path.GetTempPath() + "/temp.wav";
        if ((button.Content?.ToString() ?? "") != "Play")
        {
            var topLevel = GetTopLevel(this);
            var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Open FFmpeg binary",
                FileTypeChoices = [Filters.WavFile]
            });

            if (file is null) return;
            filePath = file.Path.AbsolutePath;
        }

        var ms = new MemoryStream();
        var loopStart = Samples[SamplesGrid.SelectedIndex].LoopStart;
        var loopEnd = Samples[SamplesGrid.SelectedIndex].LoopEnd;
        switch (button.Content)
        {
            case "Extract looping":
                ms.Write(Samples[SamplesGrid.SelectedIndex].Data.Take((int)loopStart).ToArray());
                for (int i = 0; i < 100; i++)
                {
                    ms.Write(Samples[SamplesGrid.SelectedIndex].Data.Skip((int)loopStart)
                        .Take((int)(loopEnd - loopStart)).ToArray());
                }
                ms.Write(Samples[SamplesGrid.SelectedIndex].Data.Skip((int)loopEnd).ToArray());
                break;
            case "Extract sample":
            case "Play":
                ms.Write(Samples[SamplesGrid.SelectedIndex].Data.ToArray());
                break;
        }
        ms.Position = 0;
        var decodedData = SonyVag.Decode(ms.ToArray());
        ms = new MemoryStream();
        ms.Write(decodedData);
        Pcm.WriteWavHeader(ms, false, 1, 16, 32000, (int)ms.Length);
        var fs = new FileStream(Uri.UnescapeDataString(filePath), FileMode.Create, FileAccess.Write);
        fs.Write(ms.ToArray(), 0, (int)ms.Length);
        fs.Close();
        JustPlay();
    }

    private void SampleGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ExtractLoopButton.IsEnabled = Samples[SamplesGrid.SelectedIndex].LoopStart != Samples[SamplesGrid.SelectedIndex].LoopEnd;
    }
}