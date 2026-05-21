using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.Controls;

public partial class StageLayouts : UserControl
{
    public StageLayouts()
    {
        InitializeComponent();
    }
    
    public ObservableCollection<FpnLay.Layout> LayoutSource
    {
        get => GetValue(LayoutSourceProperty);
        set => SetValue(LayoutSourceProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<FpnLay.Layout>> LayoutSourceProperty = AvaloniaProperty.Register<MenuMockup, ObservableCollection<FpnLay.Layout>>(nameof(LayoutSource));

    private async void ExportChangesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
            if (!File.Exists(mw.FileName))
            {
                mw.ShowDialog("Flipnic file tools", "This operation is not supported for files loaded directly to memory. Please open the file through File > Open menu.", NotificationType.Error);
            }
            var file = await FileHelpers.SaveFile(this, [Filters.LayFile]);
            if (file is null) return;
            var lay = new FpnLay(await File.ReadAllBytesAsync(mw.FileName!))
            {
                Layouts = new List<FpnLay.Layout>(LayoutSource)
            };
            var patchedData = lay.CommitChanges();
            await File.WriteAllBytesAsync(file, patchedData);
            mw.ShowDialog("Flipnic file tools", "File saved successfully.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            if (TopLevel.GetTopLevel(this) is not MainWindow mw) return;
            mw.ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
    }
}