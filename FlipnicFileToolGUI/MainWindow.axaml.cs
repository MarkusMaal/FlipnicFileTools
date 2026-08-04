using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FlipnicFileToolGUI.Controls;
using FlipnicFileToolGUI.Handlers;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace FlipnicFileToolGUI;

public sealed partial class MainWindow : SukiWindow
{
    // Declarations
    public static int Progress { get; set; }
    public static int ProgressMax { get; set; }


    public const string FTypeFormat = "Type: {0}";

    public bool IsLightTheme => GetViewModel().IsLightTheme;

    internal static bool ErrorDisplayed = false;

    internal readonly ISukiDialogManager DialogManager = new SukiDialogManager();

    public string? FileName { get; set; }

    public BinFile? Fs { get; set; }

    public IsoUdf? IsoFile { get; set; }
    
    // Constructor
    public MainWindow()
    {
        InitializeComponent();
        if (((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows.Count == 0)
        {
            ApplyCustomTheme();
        }

        StaticUtils.TextUpdate += value => BusyHandlers.UpdateText(value, this);
        DialogHost.Manager = DialogManager;

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragLeaveEvent, (_, e) =>
        {
            e.DragEffects = DragDropEffects.None;
        });
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, WindowDropped);
        if (!Design.IsDesignMode) { return; }
        Mocks.DisplayMocks(this);
        SukiTheme.GetInstance().SwitchBaseTheme();
        ApplyCustomTheme();
    }

    // Tabs (generic)
    private static void UpdateSpecialTabThemes()
    {
        //if (MainTabControl.SelectedItem is not SukiSideMenuItem mi) return;
    }
    private void MainTabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateSpecialTabThemes();

    
    // Tab -> Models
    private void RestartWglButton_Click(object? sender, RoutedEventArgs e) => Handlers.ModelTab.RestartWgl(this); // Open 3D preview

    private void Models_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => Handlers.ModelTab.ModelSelectionChanged(this);

    private void RotateModelCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e) // Spin
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

    private void TpModelButton_OnClick(object? sender, RoutedEventArgs e) => GlControl.Teleport(); // Teleport to model

    private void ExportModelButton_Click(object? sender, RoutedEventArgs e) // Export model/Export JSON
    {
        if (sender is not Button button) return;
        Handlers.ModelTab.ExportModelClick(button, this);
    }
    
    // Tab -> Texture
    private void SaveImgAsBtn_OnClick(object? sender, RoutedEventArgs e) => TextureTab.ExportImage(this); // Save as..

    private void TextureImageConfigChanged(object? sender, RoutedEventArgs e) => TextureTab.SetupImage(this, sender); // Radio buttons
    
    // Tab -> Sound
    private void PlayButton_OnClick(object? sender, RoutedEventArgs e) // Play/Stop
    {
        if (sender is not Button b) return;
        SoundTab.Play(b, this);
    }

    private void SaveSoundAsButton_OnClick(object? sender, RoutedEventArgs e) => SoundTab.SaveSoundAs(this); // Save as..

    // Tab -> Gimmicks
    private void ExportGimmicksButton(object? sender, RoutedEventArgs e) => GimmickTab.ExportGimmicks(this); // Export changes

    private void LocateLayoutButton_Clicked(object? sender, RoutedEventArgs e) => GimmickTab.LocateLayoutButton_Clicked(this); // Locate layout file


    private void GimmickCombobox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var val = ((ComboBox)sender!).SelectedValue?.ToString();
        if (val == null) return;
        GetViewModel().SelectedGimmick = GetViewModel().Gimmicks?[val]!;
        GimmickGrid.ItemsSource = GetViewModel().SelectedGimmick;
    }
    
    // Tab -> Samples
    private void ExtractSampleButton_OnClick(object? sender, RoutedEventArgs e) // Extract sample
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        ExtractUtils.ExtractSample(button, this);
    }

    private void SampleGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ExtractLoopButton.IsEnabled = GetViewModel().Samples![SamplesGrid.SelectedIndex].LoopStart != GetViewModel().Samples![SamplesGrid.SelectedIndex].LoopEnd;
    }
    
    // Tab -> Files
    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        OpenButton.IsEnabled = ((DataGrid)sender!).SelectedIndex != -1;
        ExtractButton.IsEnabled = ((DataGrid)sender).SelectedIndex != -1;
        ReplaceButton.IsEnabled = ((DataGrid)sender).SelectedItems.Count == 1;
    }

    private void OpenButton_OnClick(object? sender, RoutedEventArgs e) => FilesTab.OpenButton(this); // Open
    private void ExtractButton_OnClick(object? sender, RoutedEventArgs e) => ExtractUtils.Extract(this); // Extract
    private void ReplaceButton_OnClick(object? sender, RoutedEventArgs e) =>  RepackUtilsGui.ReplaceFile(this); // Replace
    private void ExtractAllButton_OnClick(object? sender, RoutedEventArgs e) => ExtractUtils.ExtractAll(this); // Extract all

    // Tab -> Convert
    private void FFmpegBox_OnTextChanged(object? sender, TextChangedEventArgs e) => Handlers.ConvertTab.FFmpegBoxUpdate(this); // FFmpeg path
    private void BrowseButtonFfmpeg_OnClick(object? sender, RoutedEventArgs e) => Handlers.ConvertTab.BrowseFfmpeg(FFmpegBox, this); // FFmpeg path -> Browse..

    private void MidiBdBox_TextChanged(object? sender, TextChangedEventArgs e) => Handlers.ConvertTab.MidiBdChanged(this); // MIDI/BD file
    private void BrowseButtonBdMidi_OnClick(object? sender, RoutedEventArgs e) // MIDI/BD file -> Browse..
    {
        if (Design.IsDesignMode || sender is not Button button) return;
        Handlers.ConvertTab.BrowseMidiBd(this, button.Name == "BrowseButtonBd");
    }

    private void BrowsButtonPath_OnClick(object? sender, RoutedEventArgs e) => Handlers.ConvertTab.BrowseFolder(FileBox, this); // Save to -> Browse..

    private void ReverbSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e) => Handlers.ConvertTab.SliderUpdate(((Slider?)e.Source)!.Name, e.NewValue, this); // Reverb/ADSR sliders

    private void DemuxButton_OnClick(object? sender, RoutedEventArgs e) // Demux
    {
        if (Design.IsDesignMode) return;
        Converters.Demux(this);
    }

    private void ConvertMovAacButton_OnClick(object? sender, RoutedEventArgs e) => Handlers.ConvertTab.ConvertMovAacButton(this); // Convert to MPEG4
    private void ConvertMovButton_OnClick(object? sender, RoutedEventArgs e) => Converters.ConvertMov(this); // Convert to .M2V 

    private void ConvertSf2Button_OnClick(object? sender, RoutedEventArgs e) => Handlers.ConvertTab.ConvertSf2Button(this); // Convert to .SF2

    // Tab -> Save data
    private void ScoreGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e) => RankGrid.ItemsSource = GetViewModel().SaveData.Rank;

    private async void ExportRecordSst_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var saveFile = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Select destination file",
                    FileTypeChoices = [Filters.FpnSst]
                });

            if (saveFile == null) return;
            var fileName = Uri.UnescapeDataString(saveFile.Path.AbsolutePath);
            GetViewModel().SaveData.Save(fileName);
            ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ShowDialog("Flipnic file tools", $"Save failed!\n\nDetails: {ex.Message}", NotificationType.Error);
        }
    }
    
    // Menus -> File
    private void NewWindowMenuItem_OnClick(object? sender, RoutedEventArgs e) // New
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
    private void OpenNativeMenuItem_OnClick(object? sender, EventArgs e) // Open (native/also used for Options -> Import JA.MSG)
    {
        if (sender is NativeMenuItem menu)
        {
            MenuHandlers.OpenMenuFromStr(menu.Header ?? "", this);
        }
    }

    private void OpenMenuItem_OnClick(object? sender, RoutedEventArgs e) // Open (also used for Options -> Import JA.MSG)
    {
        if (sender is MenuItem menu)
        {
            MenuHandlers.OpenMenuFromStr(menu.Header?.ToString() ?? "", this);
        }
    }

    private void CloseOthersMenuItem_OnClick(object? sender, RoutedEventArgs e) => MenuHandlers.CloseOtherWindows(); // Close other windows

    private void CloseOthersNativeMenuItem_OnClick(object? sender, EventArgs e) =>  MenuHandlers.CloseOtherWindows(); // Close other windows (native)

    private void OpenImHexMenuItem_OnClick(object? sender, RoutedEventArgs? e) => MenuHandlers.OpenInImhex(this); // Open in ImHex
    
    private void OpenImHexNativeMenuItemClick(object? sender, EventArgs e) => MenuHandlers.OpenInImhex(this); // Open in ImHex (native)

    private void CloseNativeMenuItem_Click(object? sender, EventArgs e) => Close(); // Close (native)

    private void CloseMenuItem_OnClick(object? sender, RoutedEventArgs e) // Close
    {
        if (Design.IsDesignMode) return;
        Close();
    }

    private void ExitMenuItem_OnClick(object? sender, RoutedEventArgs e) // Exit
    {
        if (Design.IsDesignMode) return;
        MenuHandlers.CloseOtherWindows();
        Close();
    }
    
    // Menus -> Options
    internal void PalMenuItem_OnClick(object? sender, RoutedEventArgs? e) => MenuHandlers.DarkModeToggle(this); // Toggle dark theme

    internal void ToggleDarkNative(object? sender, EventArgs e) => MenuHandlers.DarkModeToggle(this); // Toggle dark theme (native)

    private void AltNormalMethod_NativeClick(object? sender, EventArgs e) => MenuHandlers.AltNormalMethodToggle(this, sender); // Normal vectors decoding method (native)

    internal void AltNormalMethod_Click(object? sender, RoutedEventArgs? e) => MenuHandlers.AltNormalMethodToggle(this, sender); // Normal vectors decoding method
    
    // Menus -> Info

    private void DocsMenu1_OnClick(object? sender, RoutedEventArgs? e) => MenuHandlers.OpenUrl("https://github.com/MarkusMaal/FlipnicFileTools/blob/master/GUIREADME.md", this); // Tutorial

    private void DataStructsMenu1_OnClick(object? sender, RoutedEventArgs? e) => MenuHandlers.OpenUrl("https://github.com/MarkusMaal/FlipnicPatterns", this); // Flipnic data structures

    private void SaveEditorMenu1_OnClick(object? sender, RoutedEventArgs? e) => MenuHandlers.OpenUrl("https://github.com/MarkusMaal/FlipnicSaveEditor", this); // Save file editing

    private void DocsNativeMenuClick(object? sender, EventArgs e) => DocsMenu1_OnClick(sender, null); // Tutorial (native)

    private void DataStructsNativeMenuClick(object? sender, EventArgs e) => DataStructsMenu1_OnClick(sender, null); // Flipnic data structures (native)

    private void SaveEditorNativeMenuClick(object? sender, EventArgs e) => SaveEditorMenu1_OnClick(sender, null); // Save file editing (native)

    public void AboutClick(object? sender, RoutedEventArgs? e) // About
    {
        ShowDialog("Flipnic file tools",
            Program.AboutText,
            NotificationType.Information);
    }

    private void FileMenu1_OnSubmenuOpened(object? sender, RoutedEventArgs e) // Check if there are other windows open and that the file is accessible when opening file menu
    {
        OpenImHexMenuItem.IsVisible = File.Exists(FileName);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        CloseOthersMenuItem.IsVisible = al.Windows.Count > 1;
        CloseMenuItem.IsVisible = al.Windows.Count > 1;
    }
    
    private void NativeFileMenuOpening(object? sender, EventArgs e) // Check if there are other windows open when opening file menu (native)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        CloseOthersMenuItem.IsVisible = al.Windows.Count > 1;
        // update recents submenu (macOS only)
        if (sender is not NativeMenu nm) return;
        if (nm.Items.First(p => ((NativeMenuItem)p).Header == "Recent") is not NativeMenuItem nmi) return;
        nmi.IsVisible = Preferences.RecentFiles.Count > 0;
        for (var idx = 0; idx < Preferences.RecentFiles.Count; idx++)
        {
            ((NativeMenuItem)nmi.Menu!.Items[idx]).Header = new FileInfo(Preferences.RecentFiles[idx]).Name;
            if (((NativeMenuItem)nmi.Menu!.Items[idx]).IsVisible) continue;
            ((NativeMenuItem)nmi.Menu!.Items[idx]).IsVisible = true;
            var idx1 = idx;
            ((NativeMenuItem)nmi.Menu!.Items[idx]).Click += (_, _) =>
            {
                FileName = Preferences.RecentFiles[idx1];
                FileHelpers.LoadFromData(new FileStream(FileName, FileMode.Open, FileAccess.Read),
                    FileName[^3..], this);
            };
        }
    }

    // Miscellaneous
    public MainWindowViewModel GetViewModel()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            return vm;
        }

        return Design.IsDesignMode ? new MainWindowViewModel() : throw new NullReferenceException("View model is not initialized");
    }

    private static void DragOver(object? sender, DragEventArgs e) => DragDropHandlers.DragOver(e);

    internal static void ApplyCustomTheme() => SukiTheme.GetInstance().SwitchColorTheme();

    private void WindowDropped(object? sender, DragEventArgs e) => DragDropHandlers.Drop(e, this);

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        App.Init(this);
        if (Program.GpuAccel) Preferences.LoadPreferences(this);
        ReloadRecentMenu();
    }

    public void ReloadRecentMenu()
    {
        if (OperatingSystem.IsMacOS()) return;
        RecentMenuItem.Items.Clear();
        if (Preferences.RecentFiles.Count <= 0) return;
        RecentMenuItem.IsVisible = true;
        Preferences.RecentFiles.ForEach(p => RecentMenuItem.Items.Add(new FileInfo(p).Name));
        RecentMenuItem.Click += (sender, e) =>
        {
            if (sender is not MenuItem mi2 || RecentMenuItem.SelectedIndex == -1) return;
            var cF = Preferences.RecentFiles[RecentMenuItem.SelectedIndex];
            FileName = cF;
            FileHelpers.LoadFromData(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName[^3..], this);
        };
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

    private void UpdateThemeContainer(ScrollViewer cb)
    {
        if (cb.Name != "CliBox") return;
        var cliBox = (InfoBox)cb.Parent!;
        cliBox.IsLightTheme = IsLightTheme;
    }
    
    internal void ForceRefresh()
    {
        foreach (var t in MainTabControl.Items)
        {
            if (t is not SukiSideMenuItem mi) continue;
            foreach (var child in ((Control)mi.PageContent).GetLogicalChildren())
            {
                if (child is not ScrollViewer cb) continue;
                UpdateThemeContainer(cb);
            }
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return;
        if (!Program.GpuAccel && al.Windows.Count == 1) Preferences.SavePreferences(InfoBox.IsLightTheme, StaticUtils.MsgFile);
    }
}