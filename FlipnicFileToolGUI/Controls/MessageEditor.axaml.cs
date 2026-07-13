using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.Controls;

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
        if (Design.IsDesignMode)
        {
            MsgObject =  new FpnMsg
            {
                Messages = ["Message A", "Message B", "Message C"]
            };
        }
    }

    private async void ExportTxtButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await FileHelpers.SaveFile(this, [Filters.TxtFile], "Save text file");
            if (file == null) return;
            await File.WriteAllTextAsync(Uri.UnescapeDataString(file), string.Join("\n", MsgObject.Messages));
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
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
        try
        {
            var file = await FileHelpers.SaveFile(this, [Filters.FpnMsg], "Save message table");
            if (file == null) return;
            await File.WriteAllBytesAsync(Uri.UnescapeDataString(file), MsgObject.GetData());
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
    }
}