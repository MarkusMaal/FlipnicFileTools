using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace FlipnicFileToolGUI;

public sealed partial class MainWindow : SukiWindow
{
    private static int Progress { get; set; }
    private static int ProgressMax { get; set; }

    
    private const string FTypeFormat = "Type: {0}";

    private byte[] pcmData { get; set; }
    
    public ISukiDialogManager DialogManager = new SukiDialogManager();

    public static bool ErrorDisplayed = false;
    
    internal string FileName { get; set; }
    
    private BinFile Fs { get; set; }

    public ObservableCollection<string> Controls => GetViewModel().Controls;
    
    public bool IsLightTheme => GetViewModel().IsLightTheme;

    public MainWindow()
    {
        InitializeComponent();
        ApplyCustomTheme();
        SukiTheme.GetInstance().OnBaseThemeChanged += variant =>
        {
            GetViewModel().IsLightTheme = variant == ThemeVariant.Light;
            ForceRefresh();
            UpdateSpecialTabThemes();
        }; 
        DialogHost.Manager = DialogManager;
        
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragLeaveEvent, (_, e) =>
        {
            e.DragEffects = DragDropEffects.None;
        });
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, WindowDropped);
    }

    private MainWindowViewModel GetViewModel()
    {
        if (this.DataContext is MainWindowViewModel vm)
        {
            return vm; 
        }

        return new MainWindowViewModel();
        throw new NullReferenceException("View model is not initialized");
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
        if (Design.IsDesignMode) return;
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open file",
            AllowMultiple = false,
            FileTypeFilter = jaMsg ? [Filters.FpnMsg] : [Filters.AllSupported, Filters.BinFile, Filters.FpnFpc, Filters.FpnSst, Filters.FpnLp4, Filters.FpnMlb,
                Filters.SonyPss, Filters.SonyTim2, Filters.MidiFile, Filters.HdFile, Filters.VsdFile, Filters.SvagFile, Filters.TxtFile, Filters.CsvFile, Filters.XmlFile]
        });

        if (files.Count <= 0) return;
        if (jaMsg)
        {
            StaticUtils.MsgFile = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
            return;
        }
        FileName = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
        LoadFromData(new FileStream(Uri.UnescapeDataString(files[0].Path.AbsolutePath), FileMode.Open, FileAccess.Read), files[0].Path.AbsolutePath[^3..]);
        Title = "Flipnic file tool - " + new FileInfo(Uri.UnescapeDataString(files[0].Path.AbsolutePath)).Name;
    }

    private void WindowDropped(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        if (e.Data.GetFiles()?.First() == null) return;
        var fullPath = Uri.UnescapeDataString(e.Data.GetFiles()!.First().Path.AbsolutePath);
        FileName = fullPath;
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
            switch (ext.ToUpper())
            {
                case "TM2":
                    var data = new byte[ds.Length];
                    ds.ReadExactly(data);
                    ds.Position = 0;
                    var img = new Tim2(data, FileName);
                    var bt = new BitmapTools { Image = img };
                    LoadAsString(img, "PlayStation 2 texture file");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ImagePreviewTab.IsVisible = true;
                        PreviewImage.Source = bt.ToBitmap();
                    });
                    break;
                case "ICO":
                    data = new byte[ds.Length];
                    ds.ReadExactly(data);
                    ds.Position = 0;
                    var ico = new SaveIcon(data); 
                    ico.Read();
                    bt = new BitmapTools { Icon = ico.Texture };
                    
                    
                    StaticUtils.LiveLoadStatus = "Initializing OpenGL";
                    Dispatcher.UIThread.Post(() =>
                    {
                        ModelTab.IsSelected = true;
                        InfoTab.IsSelected = false;
                    });
                    Thread.Sleep(1000);
                    LoadAsString(ico, "PlayStation 2 save file icon");
                    Dispatcher.UIThread.Post(() => {
                        GlControl.ImportICO(ico);
                        Init3DStuff();
                        ModelTab.IsSelected = false;
                        ModelTab.IsVisible = true;
                        ImagePreviewTab.IsVisible = true;
                        PreviewImage.Source = bt.IconToBitmap();
                    });
                    break;
                case "MID":
                    var midi = new Midi(FileName);
                    midi.Read(ds);
                    LoadAsString(midi, "General MIDI");
                    break;
                case "VSD":
                    var vsd = new FpnVsd(File.OpenRead(FileName));
                    LoadAsString(vsd, "Vibration Strength Data");
                    break;
                case "BD": 
                case ".BD":
                    Dispatcher.UIThread.Post(() => GetViewModel().Samples = []);
                    var s = new Samples(ds);
                    var samples = new List<SampleColl>();
                    var offset = 0;
                    for (var i = 0; i < s.RawSamples.Count; i++)
                    {
                        samples.Add(new SampleColl
                        {
                            Data = s.RawSamples[i],
                            Id = i,
                            Offset = (int)offset + 0x10,
                            LoopStart = s.LoopStarts[i],
                            LoopEnd = s.LoopEnds[i],
                        });
                        offset += s.Lengths[i];
                    }

                    Dispatcher.UIThread.Post(() =>
                    {
                        GetViewModel().Samples = new ObservableCollection<SampleColl>(samples);
                        BdSampleTab.IsVisible = true;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "JAM body");
                    });
                    break;
                case "HD":
                case ".HD":
                    var jh = new JamHeader();
                    try { 
                        jh.Read(new BinaryStream(ds));
                    } catch (InvalidDataException)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ShowDialog("Flipnic File Tools", "Cannot parse this file, because it's missing the SShd header. The file may be corrupt or incompatible with this program.", NotificationType.Error);
                            InfoTab.IsVisible = true;
                            InfoBox.Text = "Error opening file!";
                            FileTypeLabel.Content = "Ready";
                            this.Title = "Flipnic file tool";
                        });
                        break;
                    }
                    LoadAsString(jh, "JAM header");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ConvertTab.IsVisible = jh.ProgramChunks.Count > 0;
                        FfmpegBrowserGrid.IsVisible = false;
                        BdBrowserGrid.IsVisible = true;
                        MidiBrowserGrid.IsVisible = true;
                        PalToggle.IsVisible = false;
                        EnvelopeToggle.IsVisible = true;
                        VelocityToggle.IsVisible = true;
                        ConvertSf2Button.IsVisible = true;
                        ConvertMovAacButton.IsVisible = false;
                        ConvertMovButton.IsVisible = false;
                        DemuxButton.IsVisible = false;

                        var fileDirectory = new FileInfo(FileName).Directory?.FullName ?? "";
                        var extension = Path.GetExtension(FileName);
                        var fileName = new FileInfo(FileName).Name.Replace(extension, "");
                        var bdPath = Path.Combine(fileDirectory, fileName) + ".BD";
                        var midPath = Path.Combine(fileDirectory, fileName) + ".MID";
                        if (File.Exists(bdPath)) BdBox.Text = bdPath;
                        if (File.Exists(midPath)) MidiBox.Text = midPath;
                    });
                    break;
                case "CSV":
                case "TXT":
                case "XML":
                    var txt = Encoding.UTF8.GetString(ds.ReadBytes((int)ds.Length));
                    LoadAsString(txt, (ext == "CSV") ? "Comma Separated Values" : ((ext == "XML") ? "eXtensible Markup Language" : "Plain Text"));
                    break;
                case "SVAG":
                case "INT":
                case "VAG":
                    var va = new byte[ds.Length];
                    ds.ReadExactly(va);
                    pcmData = SonyVag.Decode(va);
                    Dispatcher.UIThread.Post(() =>
                    {
                        SoundPlayerTab.IsVisible = true;
                        AudioFilename.Content = "Filename: " + Path.GetFileName(FileName);
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Compressed Sony ADPCM Audio " + (FileName.EndsWith("INT") ? "(Stereo)" : "(Mono)"));
                    });
                    break;
                case "MLB":
                    var mlbDa = new byte[ds.Length];
                    ds.ReadExactly(mlbDa);
                    var mlb = new FpnMlb(mlbDa);
                    StaticUtils.LiveLoadStatus = "Generating menu...";
                    Dispatcher.UIThread.Post(() => GetViewModel()._menu.Clear());
                    var menuIndex = 0;
                    Dispatcher.UIThread.Post(() => LoadProgress.IsIndeterminate = false);
                    Dispatcher.UIThread.Post(() => LoadProgress.Maximum = mlb.Sections.Count);
                    foreach (var sect in mlb.Sections)
                    {
                        try
                        {
                            var r = from ima in sect.Value
                                let p =
                                    Path.Combine(Path.GetDirectoryName(FileName) ?? string.Empty,
                                        ima.Texture.Split('\\')[^1].ToUpper())
                                let bmp =
                                    new BitmapTools { Image = new Tim2(File.ReadAllBytes(p), FileName), }.ToBitmap()
                                select new MenuElementViewModel
                                    { Layer = sect.Key, MenuElement = ima, ImageSource = bmp };
                            Dispatcher.UIThread.Post(() => GetViewModel()._menu.AddRange(r));
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
                        MenuMockup.MenuElementSource = new ObservableCollection<MenuElementViewModel>(GetViewModel()._menu);
                    });
                    LoadAsString(mlb, "Menu layout file");
                    break;
                case "LP4":
                    var lp4Da =  new byte[ds.Length];
                    ds.ReadExactly(lp4Da);
                    var lp4 = new Lp4(lp4Da, FileName);
                    LoadAsString(lp4, "Flipnic resource file");
                    StaticUtils.LiveLoadStatus = "Initializing OpenGL";
                    Dispatcher.UIThread.Post(() =>
                    {
                        ModelTab.IsSelected = true;
                        InfoTab.IsSelected = false;
                    });
                    Thread.Sleep(1000);
                    Dispatcher.UIThread.Post(() => {
                        GlControl.ImportLP4(lp4);
                        Init3DStuff();
                        ModelTab.IsSelected = false;
                        ModelTab.IsVisible = true;
                    });
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
                        EnvelopeToggle.IsVisible = false;
                        VelocityToggle.IsVisible = false;
                        ConvertSf2Button.IsVisible = false;
                        DemuxButton.IsVisible = false;
                        BdBrowserGrid.IsVisible = false;
                        MidiBrowserGrid.IsVisible = false;
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
                        GetViewModel().Gimmicks = sst.GetGimmicks();
                        StageGimmickTab.IsVisible = GetViewModel().Gimmicks?.Count > 0;
                        GimmickCombobox.Items.Clear();
                        foreach (var key in GetViewModel().Gimmicks?.Keys.ToArray() ?? [])
                        {
                            GimmickCombobox.Items.Add(key);
                        }
                        PseudoCodeTab.IsVisible = sst.TableOfContents.ContainsKey("EVENT");
                        if (PseudoCodeTab.IsVisible)
                        {
                            EventBox.Text = sst.GeneratePseudoCode();
                        }
                        if (sst.HasScoreRecord())
                        {
                            GetViewModel().SaveData = sst.GetSaveFromRecord();
                            SaveEditorTabControl.SelectedIndex = 1;
                            SaveEditor.IsVisible = true;
                            foreach (var o in SaveEditorTabControl.Items)
                            {
                                if (o is not TabItem ti) continue;
                                if ((string)(ti.Header ?? "") == "Ranking")
                                {
                                    ti.IsVisible = true;
                                    continue;
                                }
                                ti.IsVisible = false;
                            }
                        }

                        GimmickCombobox.SelectedIndex = 0;
                        InfoTab.IsVisible = true;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Stage information file");
                    });
                    break;
                case "BIN":
                    Fs = new BinFile();
                    Fs.FsEntries.Clear();
                    Fs.ListBin(ds);
                    
                    var fsEntries = Fs.FsEntries.ToList();
                    Dispatcher.UIThread.Post(() =>
                    {
                        GetViewModel().VirtualFiles = new ObservableCollection<VirtualFile>(fsEntries);
                        FileListTab.IsVisible = true;
                        FilesGrid.ItemsSource = GetViewModel().VirtualFiles;
                        FileTypeLabel.Content = string.Format(FTypeFormat, "Blob file");
                    });
                    break;
                case "PSS":
                    var pssInfo = new Pss(FileName).ListPss(ds);
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
                        EnvelopeToggle.IsVisible = false;
                        VelocityToggle.IsVisible = false;
                        ConvertSf2Button.IsVisible = false;
                        BdBrowserGrid.IsVisible = false;
                        MidiBrowserGrid.IsVisible = false;
                    });
                    break;
                default:
                    var d = ds.ReadBytes(ds.Length <= 0x2780 ? (int)ds.Length : 0x2780);
                    var sd = new FpnSave(d);
                    Dispatcher.UIThread.Post(() => GetViewModel().SaveData = sd);
                    if (!sd.isValidHeader())
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            InfoBox.Text = "Unrecognized file type";
                            FileTypeLabel.Content = string.Format(FTypeFormat, "Unknown");
                            InfoTab.IsVisible = true;
                        });
                        break;
                    }
                    Dispatcher.UIThread.Post(() =>
                    {
                        GetViewModel().SaveData = new FpnSave(d);
                        this.InfoTab.IsVisible = false;
                        this.SaveEditor.IsVisible = true;
                        SaveEditorTabControl.SelectedIndex = 0;
                        foreach (var o in SaveEditorTabControl.Items)
                        {
                            if (o is TabItem ti)
                            {
                                ti.IsVisible = true;
                            }
                        }
                        this.FileTypeLabel.Content = string.Format(FTypeFormat, "Flipnic save data");
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
        if (Design.IsDesignMode) return;
        var outPath = Path.GetTempPath() + "/temp.wav";
        PlayButton.IsEnabled = false;
        PlaybackStateLabel.Content = "Buffering";
        new Thread(() =>
        {
            StaticUtils.ConvertAudio(outPath, FileName, FileName.EndsWith("VAG"));
            Dispatcher.UIThread.Post(() => JustPlay());
        }).Start();
    }

    private void JustPlay()
    {
        if (Design.IsDesignMode) return;
        var outPath = Path.GetTempPath() + "/temp.wav";
        var player = new NetCoreAudio.Player();
        player.Play(outPath);
        Dispatcher.UIThread.Post(() => {
            PlaySampleButton.IsEnabled = false;
            PlaybackStateLabel.Content = "Now playing";
        });
        player.PlaybackFinished += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PlaybackStateLabel.Content = "Cleaning";
            });
            while (IsFileLocked(new FileInfo(outPath))) { Thread.Sleep(100); } // prevent race errors
            File.Delete(outPath);
            Dispatcher.UIThread.Post(() =>
            {
                PlaybackStateLabel.Content = "Stopped";
                PlayButton.IsEnabled = true;
                PlaySampleButton.IsEnabled = true;
            });
        };
    }

    private bool IsFileLocked(FileInfo file)
    {
        try
        {
            using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }

    private void GimmickCombobox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
       var val = ((ComboBox)sender!).SelectedValue?.ToString();
       if (val == null) return;
       var gimmickList = GetViewModel().Gimmicks?[val!];
       string[] colHeaders = ["Label", "Type", "Button", "Sound effect", "Flip. strength", "Knockback", "Bounciness"];
       List<string[]> rows = [];
       if (gimmickList == null) return;
       rows.AddRange(gimmickList.Select(entry => (string[])
       [
           entry.Label, entry.Type.ToString(), entry.Button.ToString(), entry.SoundEffect.ToString(),
           StaticUtils.DotFloatString(entry.FlipperStrength), StaticUtils.DotFloatString(entry.Knockback),
           StaticUtils.DotFloatString(entry.Bounciness)
       ]));
       GimmickBox.Text = StaticUtils.GenerateTable(colHeaders, rows, false);
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
        if (Design.IsDesignMode) return;
        this.Close();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        GetViewModel().MenuElements = new ObservableCollection<MenuElementViewModel>(GetViewModel()._menu);
        if (Design.IsDesignMode)
        {
            FileTypeLabel.Content = "Design mode";
            foreach (var tab in MainTabControl.Items)
            {
                if (tab is SukiSideMenuItem ssmi)
                {
                    ssmi.IsVisible = true;
                }
            }
            return;
        }

        InfoBox.Text = !OperatingSystem.IsLinux()
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
              or press Ctrl+Alt+V to paste a file.
              
              """;
        ForceRefresh();
        var p = new Process();
        
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.FileName = OperatingSystem.IsWindows() ? "where" : "which";
        p.StartInfo.Arguments = "ffmpeg";
        p.Start();
        DetectFromOutput(p, FFmpegBox , "FFmpeg");
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (desktop.Args?.Length == 0) return;
        if (ErrorDisplayed || desktop.Args?[0] != "-e") return;

        InfoBox.Text = $"""
            ---------------------------------
            Flipnic file tools
            ---------------------------------
            The app was restarted because of a problem.
            If this keeps re-occuring, please report it to the developer!

            {desktop.Args[1]}
            {string.Join(" ", desktop.Args.Skip(3).ToArray())}
            """;
        ErrorDisplayed = true;
    }

    private void Init3DStuff()
    {
        DispatcherTimer dpt = new();
        dpt.Interval = TimeSpan.FromMilliseconds(100);
        dpt.Tick += (_, _) =>
        {
            FPSLabel.Content = GlControl.GetInfo();
            MoreInfoLabel.Content = GlControl.GetInfo(true);
        };
        dpt.Start();
        foreach (var s in GlControl.GetVertices().Split("\n"))
        {
            Vertices.Items.Add(s);
        }

        GlControl.Focus();
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
        if (Design.IsDesignMode) return;
        new MainWindow
        {
            DataContext = new MainWindowViewModel
            {
                IsLightTheme = IsLightTheme
            },
        }.Show();
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
            Title = myTitle + vf!.Path,
            FileName = vf!.Path,
            DataContext = new MainWindowViewModel
            {
                IsLightTheme = IsLightTheme
            }
        };
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Reading data...";
            var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
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

    private void SaveFile(VirtualFile vf, string file)
    {
        if (file.Contains('*')) return;
        var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);
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
            foreach (var vf in Fs.FsEntries)
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
        if (FileBox?.Text?.Length == 0) return;
        var exist = new FileInfo(FFmpegBox?.Text ?? "/no.where").Exists;
        var exist2 = new DirectoryInfo(FileBox?.Text ?? "/no.where").Exists;
        DemuxButton.IsEnabled = exist2;
        ConvertMovAacButton.IsEnabled = exist && exist2;
        ConvertMovButton.IsEnabled = exist && exist2;
        MidiBdBox_TextChanged(sender, e);
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
        if (Design.IsDesignMode) return;
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        var outPut = FileBox.Text ?? "";
        new Thread(() =>
        {
            new Pss(FileName).ListPss(File.OpenRead(FileName), true, outPut);
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
        StaticUtils.Pal = PalToggle.IsChecked ?? false;
        var outPut = (FileBox.Text ?? "") + new FileInfo(FileName).Name + ".MOV";
        var ffMpegPath = FFmpegBox.Text ?? "";
        var originalFileName = FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Stage 1/4: Demuxing";
            new Pss(FileName).ListPss(File.OpenRead(FileName), true, new FileInfo(outPut).Directory!.FullName);
            StaticUtils.LiveLoadStatus = "Stage 2/4: Converting extracted IPU to MOV";
            var nf = Path.Combine(new FileInfo(outPut).Directory!.FullName, new FileInfo(FileName).Name);
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
                    FileName =
                        nf +
                        $".{streams}.INT";
                    StaticUtils.ConvertAudio(nf + $".{streams}.WAV", FileName);
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
                FileName = originalFileName;
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

    private void CloseNativeMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void CloseMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
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
        if (Design.IsDesignMode) return;
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
        StaticUtils.ConvertAudio(outPath, FileName, FileName.EndsWith("VAG"));
        ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
    }

    private void ConvertMovButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        StaticUtils.Pal = PalToggle.IsChecked ?? false;
        var outPut = (FileBox.Text ?? "") + new FileInfo(FileName).Name + ".MOV";
        var ffMpegPath = FFmpegBox.Text ?? "";
        var originalFileName = FileName;
        new Thread(() =>
        {
            StaticUtils.LiveLoadStatus = "Converting IPU to MOV";
            Ipu.IpuConvert(originalFileName, outPut, ffMpegPath);
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

    private void ConvertSf2Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        DockPanel1.IsVisible = false;
        Loader.IsVisible = true;
        StaticUtils.ExportVelocity = VelocityToggle.IsChecked ?? false;
        StaticUtils.ExportEnvelopes = EnvelopeToggle.IsChecked ?? false;
        var outFile = FileBox.Text;
        var midiFile = MidiBox.Text ?? "/no.where";
        var bdFile = BdBox.Text ?? "/no.where";
        new Thread(() =>
        {
            Exception? error = null;
            try
            {
                StaticUtils.LiveLoadStatus = "Converting JAM to SF2";
                var extension = Path.GetExtension(FileName);
                var fileName = new FileInfo(FileName).Name.Replace(extension, "");
                Converter.InstrumentToSoundFont2(midiFile ?? "",
                    FileName, bdFile ?? "", Path.Combine(outFile ?? "", fileName) + ".SF2");
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                error = ex;
            }

            Dispatcher.UIThread.Post(() =>
            {
                DockPanel1.IsVisible = true;
                Loader.IsVisible = false;
                if (error is null)
                {
                    ShowDialog("Flipnic file tools", "File converted successfully!", NotificationType.Success);
                    return;
                }
                ShowDialog("Flipnic file tools", "Failed to convert file. Make sure that the correct BD file was selected.\n\n" + error.Message, NotificationType.Error);
            });
        }).Start();
    }

    private async void ExtractSampleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
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
        var loopStart = GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopStart;
        var loopEnd = GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopEnd;
        if (button.Content is "Extract sample" or "Play")
        {
            ms.Write(GetViewModel().Samples[SamplesGrid.SelectedIndex].Data.ToArray());
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
        ExtractLoopButton.IsEnabled = GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopStart != GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopEnd;
    }

    private void CrashTestMenuItem_Click(object? sender, System.EventArgs e)
    {
        throw new Exception("End-user manually initiated the crash");
    }

    private void CrashMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        throw new Exception("End-user manually initiated the crash");
    }


    private async void BrowseButtonBdMidi_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        bool bdB = button.Name == "BrowseButtonBd";
        var loadFiles = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select " + (bdB ? "BD": "MIDI") + " file",
                FileTypeFilter = [bdB ? Filters.BdFile : Filters.MidiFile]
            });
        if (loadFiles.Count == 0) return;

        var fileName = Uri.UnescapeDataString(loadFiles[0].Path.AbsolutePath);
        if (bdB)
        {
            BdBox.Text = fileName;
        } else
        {
            MidiBox.Text = fileName;
        }
    }

    private void MidiBdBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ConvertSf2Button.IsEnabled = File.Exists(BdBox.Text) && File.Exists(MidiBox.Text) && Directory.Exists(FileBox.Text);
    }

    private async void ExportModelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [Filters.ObjFile]
        });

        if (file is null) return;
        GlControl.SaveAs(Uri.UnescapeDataString(file.Path.AbsolutePath));
        ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
    }

    private async void PasteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        var types = await clipboard.GetFormatsAsync();
        foreach (var type in types)
        {
            if (type != "text/uri-list") continue; 
            var data = await clipboard.GetDataAsync(type);
            if (data is not byte[] bytes) continue;
            var names = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n").Split('\n');
            var path = Uri.UnescapeDataString(new Uri(names[0]).AbsolutePath);
            FileName = path;
            var ext = new FileInfo(path).Extension;
            if (ext != "")
            {
                ext = ext[1..];
            }
            LoadFromData(new FileStream(path, FileMode.Open, FileAccess.Read), ext);
            Title = "Flipnic file tool - " + new FileInfo(path).Name;
            if (names.Length == 1) break;
            foreach (var name in names[1..])
            {
                if (name == "")  continue;
                path = Uri.UnescapeDataString(new Uri(name).AbsolutePath);
                var nw = new MainWindow() {DataContext = new MainWindowViewModel
                {
                    IsLightTheme = IsLightTheme
                }};
                nw.Title = "Flipnic file tool - " + new FileInfo(path).Name;
                nw.FileName = path;
                ext = new FileInfo(path).Extension;
                if (ext != "")
                {
                    ext = ext[1..];
                }
                nw.LoadFromData(new FileStream(path, FileMode.Open, FileAccess.Read), ext);
                nw.Show();
            }

            break;
        }
    }

    private void UpdateChecksumButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GetViewModel().SaveData.UpdateChecksum();
        ForceRefresh();
    }

    private void UpdateThemeContainer(ScrollViewer cb)
    {
        if (cb.Name != "CliBox") return;
        var cliBox = (CLIBox)cb.Parent!;
        cliBox.IsLightTheme = IsLightTheme;
    }

    private void ForceRefresh()
    {
        object? o;
        foreach (var t in MainTabControl.Items)
        {
            o = t;
            if (o is not SukiSideMenuItem mi) continue;
            foreach (var child in ((Control)mi.PageContent).GetLogicalChildren())
            {
                if (child is not ScrollViewer cb) continue;
                UpdateThemeContainer(cb);
            }
        }
        o = SaveEditorTabControl.SelectedItem;
        if (o is not TabItem tab) return;
        if (tab.GetLogicalChildren().FirstOrDefault() is not Grid g) return;
        g.DataContext = null;
        g.DataContext = GetViewModel().SaveData;
    }

    private void DiagnoseSaveFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var fixes = GetViewModel().SaveData.FixStructure();
        ForceRefresh();
        if (fixes.Length > 0)
        {
            ShowDialog("The following fixes were applied", string.Join('\n', fixes), NotificationType.Success);
            return;
        }
        ShowDialog("No fixes were applied", "Save file appears to have the correct structure", NotificationType.Information);
    }
    
    private void ScoreGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RankGrid.ItemsSource = GetViewModel().SaveData.Rank;
    }

    private void SaveEditorResetControlButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GetViewModel().SaveData.ResetControls();
        ForceRefresh();
    }

    private void SaveEditorOriginalGameRadioButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        OriginalUnlocks.IsVisible = SaveEditorOriginalGameRadioButton.IsChecked ?? false;
        FreeUnlocks.IsVisible = SaveEditorFreePlayRadioButton.IsChecked ?? false;
    }

    private void SaveEditorUnlockResetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GetViewModel().SaveData.ResetGame(SaveEditorFreePlayRadioButton.IsChecked ?? false);
        ForceRefresh();
    }

    private void StageIdComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (StageIdComboBox == null) return;
        GetViewModel().SaveData.StageId = StageIdComboBox.SelectedIndex;
        ForceRefresh();
    }

    private void SaveEditorMissionOriginalGameRadioButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (SaveEditorMissionOriginalGameRadioButton.IsChecked ?? false) GetViewModel().SaveData.DataSourceId = 0;
        if (SaveEditorMissionFreePlayRadioButton.IsChecked ?? false) GetViewModel().SaveData.DataSourceId = 1;
        if (SaveEditorMissionLastPlaythroughRadioButton.IsChecked ?? false) GetViewModel().SaveData.DataSourceId = 2;
        ForceRefresh();
    }

    private void UpdateSpecialTabThemes()
    {
        if (MainTabControl.SelectedItem is not SukiSideMenuItem mi) return;
        if (mi.Header == "Gimmicks")
        {
            GimmickBox.IsLightTheme = IsLightTheme;
        }
    }
    
    private void MainTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSpecialTabThemes();
    }
}