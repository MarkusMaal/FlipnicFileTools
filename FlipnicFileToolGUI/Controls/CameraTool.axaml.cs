using System;
using System.Globalization;
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
    public FpnFpc? CameraObject
    {
        get => GetValue(CameraObjectProperty);
        set => SetValue(CameraObjectProperty, value);
    }

    private static readonly StyledProperty<FpnFpc?> CameraObjectProperty = AvaloniaProperty.Register<CameraTool, FpnFpc?>(nameof(CameraObject));
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
        try
        {
            if (sender is not Button button) return;
            var file = await FileHelpers.SaveFile(this,
                [button.Content!.ToString()!.Contains("XML") ? Filters.XmlFile : (button.Content!.ToString()!.Contains("FPC") ? Filters.FpnFpc : Filters.TxtFile)]);
        
            if (file == null) return;
            if (CameraObject == null) return;
            if (button.Content.ToString()!.Contains("XML"))
            {
                CameraObject.GenerateXml().Save(File.OpenWrite(Uri.UnescapeDataString(file)));
            }
            else if (button.Content.ToString()!.Contains("FPC"))
            {
                await File.WriteAllBytesAsync(Uri.UnescapeDataString(file), CameraObject.GetBytes());
            }
            else
            {
                await File.WriteAllTextAsync(Uri.UnescapeDataString(file), CameraObject.ToString(false));
            }
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", $"An error has occured.\n\nDetails: {ex.Message}\n{ex.StackTrace}", NotificationType.Error);
        }
    }

    private void DataGrid_OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditingElement is not TextBox textBox) return;
        if (textBox.Text == null) return;
        var result = CameraObject?.UpdateFrame(e.Row.Index, e.Column.DisplayIndex, float.Parse(textBox.Text)) ?? 0;
        textBox.Text = result.ToString(CultureInfo.CurrentCulture);
    }
}