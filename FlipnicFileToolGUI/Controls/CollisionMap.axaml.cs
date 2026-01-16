using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.Controls;

public partial class CollisionMap : UserControl
{
    public FpnCol ColObject
    {
        get => GetValue(ColObjectProperty);
        set => SetValue(ColObjectProperty, value);
    }
    public static readonly StyledProperty<FpnCol> ColObjectProperty = AvaloniaProperty.Register<CollisionMap, FpnCol>(nameof(ColObject));
    
    public CollisionMap()
    {
        InitializeComponent();
    }

    private async void ExportClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var label = button.Name switch // find out what button the user pressed
        {
            "WtoButton" => WallList.SelectedItems?[0]?.ToString() ?? "",
            "GtoButton" => GroundList.SelectedItems?[0]?.ToString() ?? "",
            "EtoButton" => "ALL",
            _ => ""
        };
        if (label == "") return; // nobody here
        var file = await FileHelpers.SaveFile(this, [Filters.ObjFile], "Save as wavefront OBJ");
        if (file == null) return; // user didn't pick a destination
        var objData = ColObject.GenerateObj(label);
        await File.WriteAllTextAsync(Uri.UnescapeDataString(file), objData);
        ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }

    private void LbSelectChanged(object? sender, SelectionChangedEventArgs e)
    {
        // make buttons clickable if certain conditions are met
        WtoButton.IsEnabled = WallList.SelectedItems?.Count > 0;
        GtoButton.IsEnabled = GroundList.SelectedItems?.Count > 0;
    }
}