using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FlipnicLib;

namespace FlipnicFileToolGUI;

public class BitmapTools
{
    public Tim? Icon { get; init; }
    public Tim2? Image { get; init; }

    public Bitmap ToBitmap()
    {
        var ms =  new MemoryStream();
        Image?.SavePng(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    public Bitmap IconToBitmap()
    {
        var ms =  new MemoryStream();
        Icon?.SavePng(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    public byte[] ToMemoryStream()
    {
        var ms = new MemoryStream();
        if (Icon != null)
        {
            Icon?.SavePng(ms);
        }
        else
        {
            Image?.SaveBitmap(ms);
        }
        ms.Position = 0;
        return ms.ToArray();
    }
}