using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib;

namespace FlipnicFileToolGUI.Handlers;

public abstract class SoundTab
{
    public static void Play(Button b, MainWindow mw)
    {
        if (b.Content?.ToString() == "Stop")
        {
            ExtractUtils.Stop(mw);
            return;
        }
        ExtractUtils.Play(mw);
    }

    public static async void SaveSoundAs(MainWindow mw)
    {
        try
        {
            var file = await FileHelpers.SaveFile(mw, [Filters.WavFile]);
            if (file is null) return;
            var outPath = Uri.UnescapeDataString(file);
            StaticUtils.ConvertAudio(outPath, mw.FileName!, mw.FileName!.EndsWith("VAG"));
            mw.ShowDialog("Flipnic file tools", "File saved successfully!", NotificationType.Success);
        }
        catch
        {
            // ignore
        }
    }
}