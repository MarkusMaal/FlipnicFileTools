using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI;

public partial class MessageEditor : UserControl
{
    public FpnMsg MsgObject
    {
        get => GetValue(MsgObjectProperty);
        set => SetValue(MsgObjectProperty, value);
    }
    public static readonly StyledProperty<FpnMsg> MsgObjectProperty = AvaloniaProperty.Register<MessageEditor, FpnMsg>(nameof(MsgObject));
    public bool ItemSelected
    {
        get => GetValue(ItemSelectedProperty);
        set => SetValue(ItemSelectedProperty, value);
    }
    public static readonly StyledProperty<bool> ItemSelectedProperty = AvaloniaProperty.Register<MessageEditor, bool>(nameof(ItemSelected));
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MessageEditor, string>(nameof(Text));
    public MessageEditor()
    {
        InitializeComponent();
    }

    private async void ExportTxtButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save text file",
            FileTypeChoices = [Filters.TxtFile]
        });
        
        if (file == null) return;
        File.WriteAllText(Uri.UnescapeDataString(file.Path.AbsolutePath), string.Join("\n", MsgObject.Messages));
        ((MainWindow?)topLevel)?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }

    private void MessageList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        ItemSelected = lb.SelectedItems?.Count > 0;
        if (!ItemSelected) return;
        Text = lb.SelectedItems?[0]?.ToString() ?? "";
    }

    private void SelectedItemTextBox_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox tb) return;
        var idx = -1;
        ListBox? l = null;
        foreach (var c in tb.GetLogicalParent()?.GetLogicalParent()?.GetLogicalChildren() ?? [])
        {
            if (c is not ListBox lb) continue;
            idx = lb.SelectedIndex;
            l = lb;
            break;
        }

        var newObj = MsgObject;
        var arr = newObj.Messages;
        arr[idx] = tb.Text ?? "";
        newObj.Messages = arr;
        MsgObject = newObj;
        if (l == null) return;
        var bck = l.SelectedIndex;
        l.ItemsSource = MsgObject.Messages;
        l.SelectedIndex = bck;
    }

    private async void ExportMsgButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save message table",
            FileTypeChoices = [Filters.FpnMsg]
        });
        
        if (file == null) return;
        await File.WriteAllBytesAsync(Uri.UnescapeDataString(file.Path.AbsolutePath), MsgObject.GetData());
        ((MainWindow?)topLevel)?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
    }
}