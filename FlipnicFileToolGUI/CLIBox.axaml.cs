using System;
using System.Net;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FlipnicLib;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace FlipnicFileToolGUI;

public partial class CLIBox : UserControl
{
    public CLIBox()
    {
        InitializeComponent();
    }
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CircleDot, string>(nameof(Text), defaultValue: Design.IsDesignMode ? "Sample text\n\nThis text should only be displayed\nwhen designing this UserControl." : "undefined");

    private async void CopyClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, Text);
        await clipboard.SetDataObjectAsync(dataObject);
    }

    private async void SaveAsClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [Filters.TxtFile]
        });

        if (file is null) return;
        
        await System.IO.File.WriteAllTextAsync(Uri.UnescapeDataString(file.Path.AbsolutePath), Text, Encoding.UTF8);
        MainWindow.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }
}