using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.Handlers;

public abstract class ModelTab
{
    public static void Init3DStuff(MainWindow mw, Lp4? container = null)
    {
        DispatcherTimer dpt = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        dpt.Tick += (_, _) =>
        {
            mw.FPSLabel.Content = mw.GlControl.GetInfo();
            mw.MoreInfoLabel.Content = mw.GlControl.GetInfo(true);
        };
        dpt.Start();
        mw.Models.Items.Clear();
        if (container is null) return;
        foreach (var s in container.LayoutChunks?.Where(lc => lc.Hitbox?.Length > 0) ?? [])
        {
            mw.Models.Items.Add(s.Name);
        }

        if (mw.Models.Items.Count > 0)
        {
            mw.Models.SelectedIndex = 0;
        }

        mw.GlControl.Focus();
    }

    public static void ModelSelectionChanged(MainWindow mw)
    {
        if (mw.Models.SelectedIndex < 0) return;
        if (mw.Models.SelectedItems?.Count < 1) return;
        mw.GlControl.SwitchModel(mw.Models.SelectedItems?[0]?.ToString(), mw.PreviewImage);
        mw.ImagePreviewTab.IsVisible = mw.GlControl.IsTextureValid();
        mw.GlControl.ReloadModel = true;
        new Thread(() =>
        {
            var bck = false;
            var bckType = "";
            Dispatcher.UIThread.Post(() =>
            {
                bckType = mw.FileTypeLabel.Content?.ToString();
                mw.FileTypeLabel.Content = "Please wait...";
                bck = mw.ModelTab.IsSelected;
            });
            Dispatcher.UIThread.Post(() =>
            {
                mw.ModelTab.IsSelected = bck;
                mw.FileTypeLabel.Content = bckType;
            });
        }).Start();
    }

    public static async void ExportModelClick(Button button, MainWindow mw)
    {
        try
        {
            string? file;
            if (button.Name == "ExportJsonButton")
            {
                file = await FileHelpers.SaveFile(mw, [Filters.JsonFile]);
                if (file is null) return;
                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(mw.GlControl.OpenContainer, Lp4TestGenerationContext.Default.Lp4));
                mw.ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
                return;
            }
            file = await FileHelpers.SaveFile(mw, [Filters.ObjFile]);
            if (file is null) return;
            mw.GlControl.SaveAs(Uri.UnescapeDataString(file));
            mw.ShowDialog("Flipnic file tools", "File saved successfully", NotificationType.Success);
        }
        catch
        {
            // ignored
        }
    }

    public static void RestartWgl(MainWindow mw)
    {
        if (Design.IsDesignMode) return;
        Preferences.SavePreferences(mw.IsLightTheme, StaticUtils.MsgFile);
        var exePath = Environment.ProcessPath;
        if (exePath == null) return;
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            Arguments = $"\"{mw.FileName}\" --gpu"
        });
    }
}