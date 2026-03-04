using System.IO;
using Avalonia.Media.Imaging;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI;

public class BitmapTools
{
    public Tim? Icon { get; init; }
    public Tim2? Image { get; init; }

    /// <summary>
    /// Converts TIM2 to a standard bitmap
    /// </summary>
    /// <returns>Bitmap object containing the converted image</returns>
    public Bitmap ToBitmap()
    {
        var ms =  new MemoryStream();
        Image?.SavePng(ms);
        ms.Position = 0;
        try
        {
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts TIM to a standard bitmap
    /// </summary>
    /// <returns>Bitmap object containing the converted image</returns>
    public Bitmap IconToBitmap()
    {
        var ms =  new MemoryStream();
        Icon?.SavePng(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    /// <summary>
    /// Loads image to memory as bitmap data
    /// </summary>
    /// <returns>Memory stream containing the image data</returns>
    public byte[] ToMemoryStream()
    {
        var ms = new MemoryStream();
        if (Icon != null)
        {
            Icon?.SavePng(ms);
        }
        else
        {
            Image?.SavePng(ms);
        }
        ms.Position = 0;
        return ms.ToArray();
    }
}