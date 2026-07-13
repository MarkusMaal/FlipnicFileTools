using System;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using FlipnicFileToolGUI.Helpers;

namespace FlipnicFileToolGUI.Handlers;

public abstract class TextureTab
{
    public static async void ExportImage(MainWindow mw)
    {
        try
        {
            var file = await FileHelpers.SaveFile(mw, [Filters.PngFile]);
            if (file == null) return;
            ((Bitmap?)mw.PreviewImage.Source)?.Save(Uri.UnescapeDataString(file), PngBitmapEncoderOptions.Default);
            mw.ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
        }
        catch (Exception e)
        {
            mw.ShowDialog("Flipnic file tools", "Error: " + e.Message, NotificationType.Error);
        }
    }
}