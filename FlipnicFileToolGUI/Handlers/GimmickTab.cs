using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib.Formats;
using SukiUI.Dialogs;

namespace FlipnicFileToolGUI.Handlers;

public abstract class GimmickTab
{
    public static async void ExportGimmicks(MainWindow mw)
    {
        try
        {
            if (!File.Exists(mw.FileName))
            {
                mw.ShowDialog("Flipnic file tools", "This operation is not supported for files loaded directly to memory. Please open the file through File > Open menu.", NotificationType.Error);
            }
            var file = await FileHelpers.SaveFile(mw, [Filters.FpnSst]);
            if (file is null) return;
            var sst = new FpnSst(File.OpenRead(mw.FileName!));
            var patchedData = sst.PatchGimmicks(mw.GetViewModel().Gimmicks ?? []);
            await File.WriteAllBytesAsync(file, patchedData);
            mw.ShowDialog("Flipnic file tools", "File saved successfully.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            mw.ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
    }
    
    
    public static void LocateLayoutButton_Clicked(MainWindow mw)
    {
        if (mw.GimmickCombobox.SelectedItem is not string val) return;
        if (mw.FileName is null) return;
        if (val.EndsWith("GRND") || val.EndsWith("WALL"))
        {
            mw.ShowDialog("Flipnic file tools", "This gimmick collection does not have a corresponding layout file.", NotificationType.Information);
            return;
        }
        var suffix = val.EndsWith('0')
            ? $"_{val.Substring(3, 2)}_{val.Substring(5, 2)}"
            : $"_{val.Substring(3, 2)}_{val.Substring(5, 2)}_0{val.Substring(7, 1)}";
        var doesExist = File.Exists(Path.Join(new FileInfo(mw.FileName).DirectoryName, $"LAY{suffix}.LAY"));
        if (doesExist)
        {
            mw.DialogManager.CreateDialog()
                .WithTitle("Flipnic file tools")
                .WithContent(
                    $"Layout file: LAY{suffix}.LAY\n\nDo you want to open it?")
                .WithActionButton("Yes", _ =>
                {
                    var nw = new MainWindow();
                    nw.DataContext = new MainWindowViewModel
                    {
                        IsLightTheme = mw.IsLightTheme
                    };
                    MenuHandlers.DarkModeToggle(mw);
                    MenuHandlers.DarkModeToggle(mw);
                    nw.FileName = Path.Join(new FileInfo(mw.FileName).DirectoryName, $"LAY{suffix}.LAY");
                    FileHelpers.LoadFromData(new FileStream(nw.FileName, FileMode.Open, FileAccess.Read), nw.FileName[^3..], nw);
                    nw.IsMenuVisible = false;
                    nw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    nw.Show();
                }, true)
                .WithActionButton("No", _ => { }, true)
                .OfType(NotificationType.Information)
                .TryShow();
        }
        else
        {
            mw.ShowDialog("Flipnic file tools", $"Layout file: LAY{suffix}.LAY\nFile does not exist!", NotificationType.Information);
        }
    }
}