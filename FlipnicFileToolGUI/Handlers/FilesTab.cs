using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia.Threading;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.Handlers;

public abstract class FilesTab
{
    public static void OpenButton(MainWindow m)
    {
        var vf = m.FilesGrid.SelectedItem as VirtualFile;
        var myTitle = m.Title;
        var mw = new MainWindow()
        {
            Title = myTitle + vf!.Path,
            FileName = vf.Path,
            DataContext = new MainWindowViewModel
            {
                IsLightTheme = m.IsLightTheme
            }
        };

        MenuHandlers.DarkModeToggle(m);
        MenuHandlers.DarkModeToggle(m);
        new Thread(() =>
        {
            try
            {
                StaticUtils.LiveLoadStatus = "Reading data...";
                var fs = new FileStream(m.FileName!, FileMode.Open, FileAccess.Read);
                var ms = new MemoryStream();
                var buffer = new byte[vf.Length];
                fs.Seek(vf.Offset, SeekOrigin.Begin);
                fs.ReadExactly(buffer);
                fs.Close();
                ms.Write(buffer, 0, (int)vf.Length);
                ms.Position = 0;
                Dispatcher.UIThread.Post(() =>
                {
                    mw.Show();
                    FileHelpers.LoadFromData(ms, vf.Path[^3..], mw);
                });
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                StaticUtils.LiveLoadStatus = "!!!" + ex.Message + "\n" + ex.StackTrace;
            }
        }).Start();
    }
}