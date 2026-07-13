using System;
using System.IO;
using System.Linq;
using Avalonia.Input;
using FlipnicFileToolGUI.Helpers;

namespace FlipnicFileToolGUI.Handlers;

public abstract class DragDropHandlers
{
    public static void DragOver(DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Copy | DragDropEffects.Link);

        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    public static void Drop(DragEventArgs e, MainWindow mw)
    {
        if (!e.DataTransfer.Contains(DataFormat.File)) return;
        var pathAbsolutePath = e.DataTransfer.GetItems(DataFormat.File).First().TryGetFile()?.Path.AbsolutePath;
        if (pathAbsolutePath == null) return;
        var fullPath = Uri.UnescapeDataString(pathAbsolutePath);
        mw.FileName = fullPath;
        FileHelpers.LoadFromData(new FileStream(fullPath, FileMode.Open, FileAccess.Read), fullPath[^3..], mw);
    }
}