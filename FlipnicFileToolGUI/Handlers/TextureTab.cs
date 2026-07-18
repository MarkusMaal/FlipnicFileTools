using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
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

    public static void SetupImage(MainWindow mw, object? sender)
    {
        if (sender is not RadioButton rb) return;
        if (!(rb.IsChecked ?? false)) return;
        if (rb.Content == null) return;
        mw.PixelatedRadioButton.IsEnabled = (string)rb.Content != "Original size";
        mw.LinearRadioButton.IsEnabled = mw.PixelatedRadioButton.IsEnabled;
        switch (rb.Content)
        {
            case "Stretch":
                mw.PreviewImage.Stretch = Stretch.Fill;
                break;
            case "Fit":
                mw.PreviewImage.Stretch = Stretch.Uniform;
                break;
            case "Fill":
                mw.PreviewImage.Stretch = Stretch.UniformToFill;
                break;
            case "Original size":
                mw.PreviewImage.Stretch = Stretch.None;
                break;
            case "Linear":
                RenderOptions.SetBitmapInterpolationMode(mw.PreviewImage, BitmapInterpolationMode.HighQuality);
                break;
            case "Pixelated":
                RenderOptions.SetBitmapInterpolationMode(mw.PreviewImage, BitmapInterpolationMode.None);
                break;
        }
    }
}