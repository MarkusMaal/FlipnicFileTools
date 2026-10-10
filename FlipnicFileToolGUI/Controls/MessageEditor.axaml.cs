using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        SelIdxLabel.Content = $"Selection (ID: 0x{lb.SelectedIndex:X2})";
    }

    private void SelectedItemTextBox_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox) return;
        UpdateItem();
    }

    private void UpdateItem()
    {
        var idx = MessageList.SelectedIndex;
        var l = MessageList;
        var newObj = MsgObject;
        var arr = newObj.Messages;
        arr[idx] = SelectedItemTextBox.Text ?? "";
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
    

    private void InsertButton_OnClick(object? sender, RoutedEventArgs? e)
    {
        var backup = MessageList.SelectedIndex;
        MsgObject.Messages = [.. MsgObject.Messages, InsertMsgTextBox.Text ?? ""];
        InsertMsgTextBox.Text = "";
        DataContext = null;
        DataContext = this;
        if (backup < MsgObject.Messages.Length) MessageList.SelectedIndex = backup;
    }

    private void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var backup = MessageList.SelectedIndex;
        MsgObject.Messages = [.. MsgObject.Messages.Where((_, i) => i != MessageList.SelectedIndex)];
        DataContext = null;
        DataContext = this;
        if (backup < MsgObject.Messages.Length) MessageList.SelectedIndex = backup;
    }

    private void ChangeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateItem();
    }

    private void InsertMsgTextBox_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox) return;
        if (e.Key != Key.Enter) return;
        InsertButton_OnClick(sender, null);
    }

    private void UpButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var current = MessageList.SelectedIndex;
        if (current-1 < 0) return;
        var newMsg = new List<string>(MsgObject.Messages);
        (newMsg[current], newMsg[current - 1]) = (newMsg[current - 1], newMsg[current]);
        MsgObject.Messages = [.. newMsg];
        DataContext = null;
        DataContext = this;
        var newIdx = current - 1;
        if (newIdx < 0) newIdx = 0;
        MessageList.SelectedIndex = newIdx;
    }

    private void DownButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var current = MessageList.SelectedIndex;
        if (current+1 >= MsgObject.Messages.Length) return;
        var newMsg = new List<string>(MsgObject.Messages);
        (newMsg[current], newMsg[current + 1]) = (newMsg[current + 1], newMsg[current]);
        MsgObject.Messages = [.. newMsg];
        DataContext = null;
        DataContext = this;
        var newIdx = current + 1;
        if (newIdx >= MsgObject.Messages.Length) newIdx = MsgObject.Messages.Length - 1;
        MessageList.SelectedIndex = newIdx;
    }
}