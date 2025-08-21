using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FlipnicLib;

namespace FlipnicFileToolGUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void PalMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        ((MenuItem?)sender)!.IsChecked = !((MenuItem?)sender)!.IsChecked;
        StaticUtils.Pal = ((MenuItem?)sender)!.IsChecked;
    }

    private async void OpenMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open file",
            AllowMultiple = false,
            FileTypeFilter = [Filters.BinFile, Filters.FpnFpc, Filters.FpnSst, Filters.FpnLp4, Filters.FpnMlb, Filters.SonyPss, Filters.SonyTim2]
        });

        if (files.Count <= 0) return;
        foreach (var t in MainTabControl.Items)
        {
            ((TabItem)t!)!.IsVisible = false;
        }
        StaticUtils.FileName = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
        var data = await File.ReadAllBytesAsync(Uri.UnescapeDataString(files[0].Path.AbsolutePath));
        switch (files[0].Path.AbsolutePath[^3..])
        {
            case "TM2":
                var img = new Tim2(data);
                var bt = new BitmapTools() { Image = img };
                InfoTab.IsVisible = true;
                ImagePreviewTab.IsVisible = true;
                PreviewImage.Source = bt.ToBitmap();
                InfoBox.Text = img.ToString();
                break;
            case "SST":
                var sst = new FpnSst(StaticUtils.FileName);
                InfoTab.IsVisible = true;
                StageGimmickTab.IsVisible = true;
                InfoBox.Text = sst.ListEntries();
                foreach (var gimmick in sst.GetGimmicks())
                {
                    
                }
                break;
        }
    }
}