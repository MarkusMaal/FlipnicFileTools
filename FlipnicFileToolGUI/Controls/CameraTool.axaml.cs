using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.Controls;

public partial class CameraTool : UserControl
{
    public FpnFpc CameraObject
    {
        get => GetValue(CameraObjectProperty);
        set => SetValue(CameraObjectProperty, value);
    }
    public static readonly StyledProperty<FpnFpc> CameraObjectProperty = AvaloniaProperty.Register<CameraTool, FpnFpc>(nameof(CameraObject));
    public CameraTool()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            CameraObject = new FpnFpc();
        }
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var file = await FileHelpers.SaveFile(this,
            [button.Content!.ToString()!.Contains("XML") ? Filters.XmlFile : Filters.TxtFile]);
        
        if (file == null) return;
        if (button.Content.ToString()!.Contains("XML"))
        {
            CameraObject.GenerateXML().Save(File.OpenWrite(Uri.UnescapeDataString(file)));
        }
        else
        {
            await File.WriteAllTextAsync(Uri.UnescapeDataString(file), CameraObject.ToString(false));
        }
        ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }
}