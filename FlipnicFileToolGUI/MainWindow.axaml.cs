using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FlipnicFileToolGUI.Controls;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using FlipnicLib.Formats;
using FlipnicLib.Types;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace FlipnicFileToolGUI;

public sealed partial class MainWindow : SukiWindow
{
    public static int Progress { get; set; }
    public static int ProgressMax { get; set; }


    public const string FTypeFormat = "Type: {0}";

    public bool IsLightTheme => GetViewModel().IsLightTheme;

    internal static bool ErrorDisplayed = false;
    private static readonly HttpClient Client = new();

    internal readonly ISukiDialogManager DialogManager = new SukiDialogManager();

    public string? FileName { get; set; }

    public BinFile? Fs { get; set; }

    public IsoUdf? IsoFile { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        if (((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows.Count == 0)
        {
            ApplyCustomTheme();
        }

        StaticUtils.TextUpdate += (value) =>
        {   
            if (value?.StartsWith("!!!") ?? false)
            {
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    Dispatcher.UIThread.Post(() =>
                    {
                        InfoTab.IsSelected = false;
                        foreach (var t in MainTabControl.Items)
                        {
                            ((SukiSideMenuItem)t!).IsVisible = false;
                        }
                        InfoTab.IsVisible = true;
                        MainTabControl.SelectedItems.Clear();
                        MainTabControl.SelectedItems.Add(InfoTab);
                        InfoBox.Text = "A run-time error has occured\n\n" + value[3..];
                        InfoBox.WrapText = true;
                    });
                    StaticUtils.LiveLoadStatus = "";
                }).Start();
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                LoadStatus.Text = value;
                LoadProgress.IsIndeterminate = !(StaticUtils.LiveLoadStatus?.Contains('%') ?? false);
                Loader.IsVisible = value != "";
                DockPanel1.IsVisible = value == "";
                if (LoadProgress.IsIndeterminate) return;
                LoadProgress.Value = 100;
                try
                {
                    LoadProgress.Value =
                        int.Parse(StaticUtils.LiveLoadStatus!.Split(" (")[1].Split('%')[0].Split('.')[0]) / 100.0 * 28.0;
                }
                catch
                {
                    LoadProgress.Value = 0;
                }
            });
        };
        DialogHost.Manager = DialogManager;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragLeaveEvent, (_, e) =>
        {
            e.DragEffects = DragDropEffects.None;
        });
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, WindowDropped);
        if (!Design.IsDesignMode) { return; }
        PreviewImage.Source = new Bitmap(StaticUtils.GenerateCheckerboardPng(320, 240));
        ModelGrid.Background = new SolidColorBrush(Colors.Transparent);
        InfoBox.Text = "This is where the information about currently opened file will be displayed";
        EventBox.Text = "This is where the event script will be displayed";
        CameraTool.CameraObject = new FpnFpc(new MemoryStream([
            0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x70, 0x41, 0x00, 0x00, 0xF0, 0x41, 0x00, 0x00, 0xA1, 0x42, 0x00, 0x00, 0xB4, 0x42, 0x00, 0x80, 0xC8, 0x42,
            0x00, 0x80, 0xA1, 0x42, 0xCD, 0xCC, 0xF0, 0x41, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0xA0, 0x40,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xA0, 0xC0
        ]));
        SukiTheme.GetInstance().SwitchBaseTheme();
        ApplyCustomTheme();
    }

    public MainWindowViewModel GetViewModel()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            return vm;
        }

        return Design.IsDesignMode ? new MainWindowViewModel() : throw new NullReferenceException("View model is not initialized");
    }

    private static void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    internal void ApplyCustomTheme()
    {
        SukiTheme.GetInstance().SwitchColorTheme();
    }

    internal void PalMenuItem_OnClick(object? sender, RoutedEventArgs? e)
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
        ApplyCustomTheme();
        
        var windows = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows;
        GetViewModel().IsLightTheme = !GetViewModel().IsLightTheme;
        foreach (var window in windows ?? [])
        {
            if (window is not MainWindow mainWindow) continue;
            mainWindow.InfoBox.IsLightTheme = GetViewModel().IsLightTheme;
            mainWindow.EventBox.IsLightTheme = GetViewModel().IsLightTheme;
        }
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
        var file = await FileHelpers.OpenFile(this, jaMsg
            ? [Filters.FpnMsg]
            :
            [
                Filters.AllSupported,
                Filters.BdFile,
                Filters.BinFile,
                Filters.SysCnf,
                Filters.ColFile,
                Filters.CsvFile,
                Filters.DummyFile,
                Filters.FpnFpc,
                Filters.FpdFile,
                Filters.FtlFile,
                Filters.HdFile,
                Filters.SaveIcon,
                Filters.IpuFile,
                Filters.IsoFile,
                Filters.LayFile,
                Filters.LitFile,
                Filters.FpnLp4,
                Filters.MidiFile,
                Filters.FpnMlb,
                Filters.FpnMsg,
                Filters.SonyPss,
                Filters.SccFile,
                Filters.GameElf,
                Filters.FpnSst,
                Filters.SvagFile,
                Filters.SonyTim2,
                Filters.TxtFile,
                Filters.VsdFile,
                Filters.XmlFile
            ]);
        if (file == null) return;
        if (jaMsg)
        {
            StaticUtils.MsgFile = file;
            return;
        }
        FileName = file;
        FileHelpers.LoadFromData(new FileStream(file, FileMode.Open, FileAccess.Read), file[^3..], this);
    }

    private void WindowDropped(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;
        var pathAbsolutePath = e.DataTransfer.GetItems(DataFormat.File).First().TryGetFile()?.Path.AbsolutePath;
        if (pathAbsolutePath == null) return;
        var fullPath = Uri.UnescapeDataString(pathAbsolutePath);
        FileName = fullPath;
        FileHelpers.LoadFromData(new FileStream(fullPath, FileMode.Open, FileAccess.Read), fullPath[^3..], this);
    }


    private void GimmickCombobox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var val = ((ComboBox)sender!).SelectedValue?.ToString();
        if (val == null) return;
        GetViewModel().SelectedGimmick = GetViewModel().Gimmicks?[val]!;
        GimmickGrid.ItemsSource = GetViewModel().SelectedGimmick;
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
        CloseOtherWindows();
        Close();
    }

    private void CloseOtherWindows()
    {
        var windows = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows;
        while (windows?.Count > 1)
        {
            foreach (var window in windows)
            {
                if (window.IsActive) continue;
                window.Close();
                break;
            }
        }
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        App.Init(this);
        if (Program.GpuAccel) Preferences.LoadPreferences(this);
    }

    public void Init3DStuff(Lp4? container = null)
    {
        DispatcherTimer dpt = new();
        dpt.Interval = TimeSpan.FromMilliseconds(100);
        dpt.Tick += (_, _) =>
        {
            FPSLabel.Content = GlControl.GetInfo();
            MoreInfoLabel.Content = GlControl.GetInfo(true);
        };
        dpt.Start();
        Models.Items.Clear();
        if (container is null) return;
        foreach (var s in container.LayoutChunks?.Where(lc => lc.Hitbox?.Length > 0) ?? [])
        {
            Models.Items.Add(s.Name);
        }

        if (Models.Items.Count > 0)
        {
            Models.SelectedIndex = 0;
        }

        GlControl.Focus();
    }

    private void NewWindowMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        var nw = new MainWindow
        {
            DataContext = new MainWindowViewModel
            {
                IsLightTheme = IsLightTheme
            },
        };
        //nw.ToggleDarkNative(sender, null);
        nw.Show();
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        OpenButton.IsEnabled = ((DataGrid)sender!).SelectedIndex != -1;
        ExtractButton.IsEnabled = ((DataGrid)sender).SelectedIndex != -1;
        ReplaceButton.IsEnabled = ((DataGrid)sender).SelectedItems.Count == 1;
    }

    private void OpenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var vf = FilesGrid.SelectedItem as VirtualFile;
        var myTitle = Title;
        var mw = new MainWindow()
        {
            Title = myTitle + vf!.Path,
            FileName = vf.Path,
            DataContext = new MainWindowViewModel
            {
                IsLightTheme = IsLightTheme
            }
        };
        mw.ToggleDarkNative(sender, null);
        mw.ToggleDarkNative(sender, null);
        new Thread(() =>
        {
            try
            {
                StaticUtils.LiveLoadStatus = "Reading data...";
                var fs = new FileStream(FileName!, FileMode.Open, FileAccess.Read);
                var ms = new MemoryStream();
                var buffer = new byte[vf.Length];
                fs.Seek(vf.Offset, SeekOrigin.Begin);
                fs.ReadExactly(buffer);
                fs.Close();
                ms.Write(buffer, 0, (int)vf.Length);
                ms.Position = 0;
                Dispatcher.UIThread.Post(() =>
                {
                    mw.Show();
                    FileHelpers.LoadFromData(ms, vf.Path[^3..], mw);
                });
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                StaticUtils.LiveLoadStatus = "!!!" + ex.Message + "\n" + ex.StackTrace;
            }
        }).Start();
    }

    private void ExtractButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ExtractUtils.Extract(this);
    }


    private void ExtractAllButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ExtractUtils.ExtractAll(this);
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
        var file = await FileHelpers.OpenFile(this, [Filters.Executable], "Open FFmpeg binary");
        if (file == null) return;
        FFmpegBox.Text = file;
    }

    private async void BrowsButtonPath_OnClick(object? sender, RoutedEventArgs e)
    {
        var outputDir = await FileHelpers.SelectFolder(this);
        if (outputDir == null) return;
        FileBox.Text = outputDir;
    }

    private void DemuxButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        Converters.Demux(this);
    }

    private void ConvertMovAacButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DialogManager.CreateDialog()
            .WithTitle("Warning")
            .WithContent("This operation is lossy meaning the video quality may be reduced. For lossless conversion, please demux the file first and convert the streams separately.\n\nAre you sure you want to continue?")
            .WithActionButton("Yes", _ => { Converters.ConvertMovAac(this); }, true)
            .WithActionButton("No", _ => { }, true)
            .OfType(NotificationType.Warning)
            .TryShow();
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
            .WithActionButton("OK", _ => { }, true)
            .OfType(type)
            .TryShow();
    }

    private async void SaveImgAsBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await FileHelpers.SaveFile(this, [Filters.PngFile]);
        if (file == null) return;
        ((Bitmap?)PreviewImage.Source)?.Save(Uri.UnescapeDataString(file));
        ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
    }

    private void ToggleDarkNative(object? sender, EventArgs e)
    {
        PalMenuItem_OnClick(sender, null);
    }

    private void PlayButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.Content?.ToString() == "Stop")
        {
            ExtractUtils.Stop(this);
            return;
        }
        ExtractUtils.Play(this);
    }

    private async void SaveSoundAsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await FileHelpers.SaveFile(this, [Filters.WavFile]);
        if (file is null) return;
        var outPath = Uri.UnescapeDataString(file);
        StaticUtils.ConvertAudio(outPath, FileName!, FileName!.EndsWith("VAG"));
        ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
    }

    private void ConvertMovButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Converters.ConvertMov(this);
    }

    private void ConvertSf2Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        Converters.ConvertSf2(this);
        new Thread(() =>
        {
            var visible = false;
            while (!visible)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    visible = MainTabControl.IsVisible;
                });
            }
        }).Start();
    }

    private void ExtractSampleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        ExtractUtils.ExtractSample(button, this);
    }

    private void SampleGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ExtractLoopButton.IsEnabled = GetViewModel().Samples![SamplesGrid.SelectedIndex].LoopStart != GetViewModel().Samples![SamplesGrid.SelectedIndex].LoopEnd;
    }

    private async void BrowseButtonBdMidi_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        var bdB = button.Name == "BrowseButtonBd";
        var loadFiles = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select " + (bdB ? "BD" : "MIDI") + " file",
                FileTypeFilter = [bdB ? Filters.BdFile : Filters.MidiFile]
            });
        if (loadFiles.Count == 0) return;

        var fileName = Uri.UnescapeDataString(loadFiles[0].Path.AbsolutePath);
        if (bdB)
        {
            BdBox.Text = fileName;
        }
        else
        {
            MidiBox.Text = fileName;
        }
    }

    private void MidiBdBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ConvertSf2Button.IsEnabled = File.Exists(BdBox.Text) && File.Exists(MidiBox.Text) && Directory.Exists(FileBox.Text);
    }

    private async void ExportModelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        string? file;
        if (button.Name == "ExportJsonButton")
        {
            file = await FileHelpers.SaveFile(this, [Filters.JsonFile]);
            if (file is null) return;
            await File.WriteAllTextAsync(file, JsonSerializer.Serialize(GlControl.OpenContainer, Lp4TestGenerationContext.Default.Lp4));
            ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
            return;
        }
        file = await FileHelpers.SaveFile(this, [Filters.ObjFile]);
        if (file is null) return;
        GlControl.SaveAs(Uri.UnescapeDataString(file));
        ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
    }

    private void UpdateThemeContainer(ScrollViewer cb)
    {
        if (cb.Name != "CliBox") return;
        var cliBox = (CLIBox)cb.Parent!;
        cliBox.IsLightTheme = IsLightTheme;
    }

    internal void ForceRefresh()
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
    }

    private void ScoreGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RankGrid.ItemsSource = GetViewModel().SaveData.Rank;
    }

    private static void UpdateSpecialTabThemes()
    {
        //if (MainTabControl.SelectedItem is not SukiSideMenuItem mi) return;
    }

    private void MainTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSpecialTabThemes();
    }

    private void Models_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Models.SelectedIndex < 0) return;
        if (Models.SelectedItems?.Count < 1) return;
        GlControl.SwitchModel(Models.SelectedItems?[0]?.ToString(), PreviewImage);
        ImagePreviewTab.IsVisible = GlControl.IsTextureValid();
        GlControl.ReloadModel = true;
        new Thread(() =>
        {
            var bck = false;
            var bckType = "";
            Dispatcher.UIThread.Post(() =>
            {
                bckType = FileTypeLabel.Content?.ToString();
                FileTypeLabel.Content = "Please wait...";
                bck = ModelTab.IsSelected;
            });
            Dispatcher.UIThread.Post(() =>
            {
                ModelTab.IsSelected = bck;
                FileTypeLabel.Content = bckType;
            });
        }).Start();
    }

    private void ReplaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RepackUtilsGui.ReplaceFile(this);
    }

    private void ReverbSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        switch (((Slider?)e.Source)!.Name)
        {
            case "ReverbSlider":
                ReverbStrengthLabel.Content = $"Reverb strength: {Math.Round(e.NewValue / 10.0, 1)}%";
                break;
            case "AttackSlider":
                AttackMultiplierLabel.Content = $"Attack strength: {Math.Round(e.NewValue / 10.0, 1)}%";
                break;
            case "SustainSlider":
                SustainMultiplierLabel.Content = $"Sustain strength: {Math.Round(e.NewValue / 10.0, 1)}%";
                break;
            case "DecaySlider":
                DecayMultiplierLabel.Content = $"Decay strength: {Math.Round(e.NewValue / 10.0, 1)}%";
                break;
            case "ReleaseSlider":
                ReleaseMultiplierLabel.Content = $"Release strength: {Math.Round(e.NewValue / 10.0, 1)}%";
                break;
        }
    }

    private void RotateModelCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        if (cb.IsChecked is null) return;
        GlControl.Rotate = (bool)cb.IsChecked;
    }

    private void GlControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        if (GetTopLevel(this) is MainWindow mw)
        {
            mw.ModelGrid.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    private void TpModelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GlControl.Teleport();
    }

    private void AltNormalMethod_NativeClick(object? sender, EventArgs e)
    {
        if (sender is not NativeMenuItem nmi) return;
        StaticUtils.AlternateNormals = !StaticUtils.AlternateNormals;
        var letter = StaticUtils.AlternateNormals ? "B" : "A";
        nmi.Header = $"Normal vectors decoding: Method {letter}";
        if (FileName is null) return;
        FileHelpers.LoadFromData(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName[^3..], this);
    }

    internal void AltNormalMethod_Click(object? sender, RoutedEventArgs? e)
    {
        if (sender is not MenuItem mi) return;
        StaticUtils.AlternateNormals = !StaticUtils.AlternateNormals;
        var letter = StaticUtils.AlternateNormals ? "B" : "A";
        mi.Header = $"Normal vectors decoding: Method {letter}";
        if (FileName is null) return;
        FileHelpers.LoadFromData(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName[^3..], this);
    }

    private void DocsMenu1_OnClick(object? sender, RoutedEventArgs? e)
    {
        OpenUrl("https://github.com/MarkusMaal/FlipnicFileTools/blob/master/GUIREADME.md");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                ShowDialog("Error", $"Couldn't open URL. Please visit it manually:\n\n{url}", NotificationType.Error);
            }
        }
    }

    private void DataStructsMenu1_OnClick(object? sender, RoutedEventArgs? e)
    {
        OpenUrl("https://github.com/MarkusMaal/FlipnicPatterns");
    }

    private void SaveEditorMenu1_OnClick(object? sender, RoutedEventArgs? e)
    {
        OpenUrl("https://github.com/MarkusMaal/FlipnicSaveEditor");
    }

    private void OpenImHexMenuItem_OnClick(object? sender, RoutedEventArgs? e)
    {
        if (!File.Exists(FileName)) return;
        new Thread(async void () =>
        {
            try
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo("imhex")
                    {
                        Arguments = "--open \"" + FileName + "\""
                    }
                }.Start();
                Thread.Sleep(1000);
                var patternUrl = FileName[^3..].ToUpper() switch
                {
                    "LP4" => "lp4.hexpat",
                    "BIN" => "binfile.hexpat",
                    "FPC" => "fpc.hexpat",
                    ".HD" => "hd.hexpat",
                    "IPU" => "ipu.hexpat",
                    "MSG" => "msg.hexpat",
                    "PSS" => "pss.hexpat",
                    "SCC" => "scc.hexpat",
                    "SST" => "sst.hexpat",
                    "TM2" => "tim2.hexpat",
                    "ICO" => "ico.hexpat",
                    "LIT" => "lit.hexpat",
                    "VSD" => "vsd.hexpat",
                    "COL" => "col.hexpat",
                    "FPD" => "fpd.hexpat",
                    "FTL" => "ftl.hexpat",
                    "MLB" => "mlb.hexpat",
                    "LAY" => "LAY.hexpat",
                    _ => ""
                };
                if (patternUrl == "") return;
                var tmpFile = Path.GetTempFileName();
                await DownloadFile(
                    $"https://raw.githubusercontent.com/MarkusMaal/FlipnicPatterns/refs/heads/main/patterns/{patternUrl}",
                    tmpFile);
                new Process()
                {
                    StartInfo = new ProcessStartInfo("imhex")
                    {
                        Arguments = "--pattern \"" + tmpFile + "\""
                    }
                }.Start();
            }
            catch
            {
                // ignore
            }
        }).Start();
    }

    private void FileMenu1_OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        OpenImHexMenuItem.IsVisible = File.Exists(FileName);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        CloseOthersMenuItem.IsVisible = al.Windows.Count > 1;
        CloseMenuItem.IsVisible = al.Windows.Count > 1;
    }

    private static async Task<byte[]?> GetUrlContent(string url)
    {
        using var result = await Client.GetAsync(url);
        return result.IsSuccessStatusCode ? await result.Content.ReadAsByteArrayAsync() : null;
    }

    private static async Task DownloadFile(string url, string pathToSave)
    {
        var content = await GetUrlContent(url);
        if (content != null)
        {
            await File.WriteAllBytesAsync($"{pathToSave}", content);
        }
    }

    private void OpenImHexNativeMenuItemClick(object? sender, EventArgs e)
    {
        OpenImHexMenuItem_OnClick(sender, null);
    }

    private void DocsNativeMenuClick(object? sender, EventArgs e)
    {
        DocsMenu1_OnClick(sender, null);
    }

    private void DataStructsNativeMenuClick(object? sender, EventArgs e)
    {
        DataStructsMenu1_OnClick(sender, null);
    }

    private void SaveEditorNativeMenuClick(object? sender, EventArgs e)
    {
        SaveEditorMenu1_OnClick(sender, null);
    }

    private async void ExportGimmicksButton(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(FileName))
            {
                ShowDialog("Flipnic file tools", "This operation is not supported for files loaded directly to memory. Please open the file through File > Open menu.", NotificationType.Error);
            }
            var file = await FileHelpers.SaveFile(this, [Filters.FpnSst]);
            if (file is null) return;
            var sst = new FpnSst(File.OpenRead(FileName!));
            var patchedData = sst.PatchGimmicks(GetViewModel().Gimmicks ?? []);
            await File.WriteAllBytesAsync(file, patchedData);
            ShowDialog("Flipnic file tools", "File saved successfully.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
    }

    private void LocateLayoutButton_Clicked(object? sender, RoutedEventArgs e)
    {
        if (GimmickCombobox.SelectedItem is not string val) return;
        if (FileName is null) return;
        if (val.EndsWith("GRND") || val.EndsWith("WALL"))
        {
            ShowDialog("Flipnic file tools", "This gimmick collection does not have a corresponding layout file.", NotificationType.Information);
            return;
        }
        var suffix = val.EndsWith('0')
            ? $"_{val.Substring(3, 2)}_{val.Substring(5, 2)}"
            : $"_{val.Substring(3, 2)}_{val.Substring(5, 2)}_0{val.Substring(7, 1)}";
        var doesExist = File.Exists(Path.Join(new FileInfo(FileName).DirectoryName, $"LAY{suffix}.LAY"));
        if (doesExist)
        {
            DialogManager.CreateDialog()
                .WithTitle("Flipnic file tools")
                .WithContent(
                    $"Layout file: LAY{suffix}.LAY\n\nDo you want to open it?")
                .WithActionButton("Yes", _ =>
                {
                    var nw = new MainWindow();
                    nw.DataContext = new MainWindowViewModel
                    {
                        IsLightTheme = IsLightTheme
                    };
                    nw.ToggleDarkNative(sender, null);
                    nw.ToggleDarkNative(sender, null);
                    nw.FileName = Path.Join(new FileInfo(FileName).DirectoryName, $"LAY{suffix}.LAY");
                    FileHelpers.LoadFromData(new FileStream(nw.FileName, FileMode.Open, FileAccess.Read), nw.FileName[^3..], nw);
                    nw.IsMenuVisible = false;
                    nw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    nw.Show();
                }, true)
                .WithActionButton("No", _ => { }, true)
                .OfType(NotificationType.Information)
                .TryShow();
        }
        else
        {
            ShowDialog("Flipnic file tools", $"Layout file: LAY{suffix}.LAY\nFile does not exist!", NotificationType.Information);
        }
    }

    private void RestartWglButton_Click(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode) return;
        Preferences.SavePreferences(IsLightTheme, StaticUtils.MsgFile);
        var exePath = Environment.ProcessPath;
        if (exePath == null) return;
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            Arguments = $"\"{FileName}\" --gpu"
        });
    }

    private void CloseOthersMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseOtherWindows();
    }

    private void CloseOthersNativeMenuItem_OnClick(object? sender, EventArgs e)
    {
        CloseOtherWindows();
    }

    private void NativeFileMenuOpening(object? sender, EventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        CloseOthersMenuItem.IsVisible = al.Windows.Count > 1;
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        if (!Program.GpuAccel && al.Windows.Count == 1) Preferences.SavePreferences(InfoBox.IsLightTheme, StaticUtils.MsgFile);
    }
}