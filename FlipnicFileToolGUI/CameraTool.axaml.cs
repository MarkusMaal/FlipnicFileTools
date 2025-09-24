using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI;

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
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [button.Content!.ToString()!.Contains("XML") ? Filters.XmlFile : Filters.TxtFile]
        });
        
        if (file == null) return;
        if (button.Content.ToString()!.Contains("XML"))
        {
            CameraObject.GenerateXML().Save(File.OpenWrite(Uri.UnescapeDataString(file.Path.AbsolutePath)));
        }
        else
        {
            await File.WriteAllTextAsync(Uri.UnescapeDataString(file.Path.AbsolutePath), CameraObject.ToString(false));
        }
        ((MainWindow?)topLevel)?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }
}