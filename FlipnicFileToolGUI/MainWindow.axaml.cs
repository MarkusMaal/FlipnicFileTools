using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Primitives;

namespace FlipnicFileToolGUI;

public sealed partial class MainWindow : SukiWindow
{
    public static int Progress { get; set; }
    public static int ProgressMax { get; set; }

    
    public const string FTypeFormat = "Type: {0}";

    public ObservableCollection<string> Controls => GetViewModel().Controls;
    
    public bool IsLightTheme => GetViewModel().IsLightTheme;

    internal static bool ErrorDisplayed = false;
    
    private readonly ISukiDialogManager _dialogManager = new SukiDialogManager();

    public byte[]? PcmData { get; set; }
    
    public string? FileName { get; set; }
    
    public BinFile? Fs { get; set; }
    
    public IsoUdf? IsoFile { get; set; }

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
        DialogHost.Manager = _dialogManager;
        
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragLeaveEvent, (_, e) =>
        {
            e.DragEffects = DragDropEffects.None;
        });
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, WindowDropped);
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

        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
    
    private static void ApplyCustomTheme()
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
        var file = await FileHelpers.OpenFile(this, jaMsg
            ? [Filters.FpnMsg]
            :
            [
                Filters.AllSupported, Filters.BinFile, Filters.FpnFpc, Filters.FpnSst, Filters.FpnLp4, Filters.FpnMlb,
                Filters.SonyPss, Filters.SonyTim2, Filters.MidiFile, Filters.HdFile, Filters.VsdFile, Filters.SvagFile,
                Filters.TxtFile, Filters.CsvFile, Filters.XmlFile, Filters.SaveIcon
            ]);
        if (file == null) return;
        if (jaMsg)
        {
            StaticUtils.MsgFile = file;
            return;
        }
        FileName = file;
        FileHelpers.LoadFromData(new FileStream(file, FileMode.Open, FileAccess.Read), file[^3..], this);
        Title = "Flipnic file tool - " + new FileInfo(file).Name;
    }

    private void WindowDropped(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        if (e.Data.GetFiles()?.First() == null) return;
        var fullPath = Uri.UnescapeDataString(e.Data.GetFiles()!.First().Path.AbsolutePath);
        FileName = fullPath;
        FileHelpers.LoadFromData(new FileStream(fullPath, FileMode.Open, FileAccess.Read), fullPath[^3..], this);
        Title = "Flipnic file tool - " + new FileInfo(fullPath).Name;
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
        Close();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        App.Init(this);
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
        foreach (var s in container.Models)
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
        ReplaceButton.IsEnabled = ((DataGrid)sender!).SelectedItems.Count == 1;
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
                FileHelpers.LoadFromData(ms, vf.Path[^3..], mw);
            });
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
        _dialogManager.CreateDialog()
            .WithTitle("Warning")
            .WithContent("This operation is lossy meaning the video quality may be reduced. For lossless conversion, please demux the file first and convert the streams separately.")
            .WithActionButton("Yes", _ => { Converters.ConvertMovAac(this);}, true)
            .WithActionButton("No", _ => {}, true)
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
        _dialogManager.CreateDialog()
            .WithTitle(title)
            .WithContent(content)
            .WithActionButton("OK", _ => {}, true)
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
        ExtractUtils.Play(this);
    }

    private async void SaveSoundAsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await FileHelpers.SaveFile(this, [Filters.WavFile]);
        if (file is null) return;
        var outPath = Uri.UnescapeDataString(file);
        StaticUtils.ConvertAudio(outPath, FileName, FileName.EndsWith("VAG"));
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
        new Thread(() => {
            var visible = false;
            while (!visible)
            {
                Thread.Sleep(100);
                Dispatcher.UIThread.Post(() =>
                {
                    LoadStatus.Text = StaticUtils.LiveLoadStatus;
                    visible = MainTabControl.IsVisible;
                });
            }
        });
    }

    private async void ExtractSampleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        ExtractUtils.ExtractSample(button, this);
    }

    private void SampleGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ExtractLoopButton.IsEnabled = GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopStart != GetViewModel().Samples[SamplesGrid.SelectedIndex].LoopEnd;
    }

    private void CrashTestMenuItem_Click(object? sender, EventArgs e)
    {
        throw new Exception("End-user manually initiated the crash");
    }

    private void CrashMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        throw new Exception("End-user manually initiated the crash");
    }


    private async void BrowseButtonBdMidi_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        var bdB = button.Name == "BrowseButtonBd";
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

    private async void ExportModelButton_Click(object? sender, RoutedEventArgs e)
    {
        var file = await FileHelpers.SaveFile(this, [Filters.ObjFile]);
        if (file is null) return;
        GlControl.SaveAs(Uri.UnescapeDataString(file));
        ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
    }

    private void PasteMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        FileHelpers.PasteFile(this, clipboard);
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

    private void Models_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Models.SelectedIndex < 0) return;
        GlControl.SwitchModel(Models.SelectedItems?[0]?.ToString(), PreviewImage);
        new Thread(() =>
        {
            var bck = false;
            var bckType = "";
            Dispatcher.UIThread.Post(() =>
            {
                Loader.IsVisible = true;
                MainTabControl.IsVisible = false;
                LoadStatus.Text = "Generating model";
                bckType = FileTypeLabel.Content?.ToString();
                FileTypeLabel.Content = "Please wait...";
                bck = ModelTab.IsSelected;
                ModelTab.IsSelected = false;
                ModelTab.IsVisible = false;
            });
            Thread.Sleep(800);   
            Dispatcher.UIThread.Post(() => {
                Loader.IsVisible = false;
                MainTabControl.IsVisible = true;
                ModelTab.IsVisible = true;
                ModelTab.IsSelected = bck;
                FileTypeLabel.Content = bckType;
                LoadStatus.Text = "Loading";
            });
        }).Start();
    }

    private async void ReplaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (FilesGrid.SelectedItem is not VirtualFile vf) return;
        if (FileName is null) return;
        var replacement = await FileHelpers.OpenFile(this, [], "Choose a replacement file");
        if (replacement == null) return;
        if (FileName.ToUpper().EndsWith(".ISO"))
        {
            new Thread(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Loader.IsVisible = true;
                    MainTabControl.IsVisible = false;
                });
                new IsoUdf(FileName).ReplaceFile(replacement, FileName, vf.Path);
                Dispatcher.UIThread.Post(() =>
                {
                    Loader.IsVisible = false;
                    MainTabControl.IsVisible = true;
                    StaticUtils.LiveLoadStatus = "Done!";
                    ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
                });
            }).Start();
            new Thread(() =>
            {
                while (StaticUtils.LiveLoadStatus != "Done!")
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        LoadStatus.Text = StaticUtils.LiveLoadStatus;
                    });
                    Thread.Sleep(100);
                }
                StaticUtils.LiveLoadStatus = "Please wait...";
            }).Start();
            return;
        }
        var offset = vf.Offset;
        var size = vf.Length;
        var rfi = new FileInfo(replacement);
        var binFiles = new BinFile().GetListBin(File.OpenRead(replacement));
        if (rfi.Length > size)
        {
            var nSize = new FileInfo(FileName).Length;
            while ((nSize - vf.Length) % 0x800 != 0)
            {
                nSize++;
            }
            _dialogManager.CreateDialog()
                .WithTitle("CAUTION")
                .WithContent("It appears the replacement file is bigger than the original file. We will need to update other file records and increase the size of the .BIN file. This should only be done if you know exactly what you're doing. Are you sure you want to continue?")
                .WithActionButton("Yes", _ => {
                    Loader.IsVisible = true;
                    MainTabControl.IsVisible = false;
                    LoadStatus.Text = "Rebuilding .BIN file";
                    new Thread(() =>
                    {
                        RepackUtils.ResizeFile(vf.Path, (int)nSize, File.Open(FileName, FileMode.Open), binFiles);
                        RepackUtils.RepackFileUnsafe(offset, replacement, FileName, size, vf.Path[1..].Contains('\\') && !vf.Path[1..].EndsWith('\\') ? 1 : 2048);

                        Dispatcher.UIThread.Post(() => {
                            ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
                            Loader.IsVisible = false;
                            MainTabControl.IsVisible = true;
                        });
                    }).Start();
                }, true)
                .WithActionButton("No", _ => {
                    new Thread(() =>
                    {
                        Thread.Sleep(200);
                        Dispatcher.UIThread.Post(() => ShowDialog("Flipnic file tools", "No changes were made.", NotificationType.Information));
                    }).Start();
                }, true)
                .OfType(NotificationType.Warning)
                .TryShow();
            return;
        }
        RepackUtils.RepackFileUnsafe(offset, replacement, FileName, size, vf.Path[1..].Contains('\\') && !vf.Path[1..].EndsWith('\\') ? 1 : 2048);
        ShowDialog("Flipnic file tools", "File replaced successfully.", NotificationType.Success);
    }

    private void ReverbSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        ReverbStrengthLabel.Content = $"Reverb strength: {Math.Round(e.NewValue/10.0, 1)}%";
    }
}