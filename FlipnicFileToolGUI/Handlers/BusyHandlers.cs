using System.Threading;
using Avalonia.Threading;
using FlipnicLib;
using SukiUI.Controls;

namespace FlipnicFileToolGUI.Handlers;

public abstract class BusyHandlers
{
    public static void UpdateText(string? value, MainWindow mw)
    {   
        if (value?.StartsWith("!!!") ?? false)
        {
            new Thread(() =>
            {
                Thread.Sleep(500);
                Dispatcher.UIThread.Post(() =>
                {
                    mw.InfoTab.IsSelected = false;
                    foreach (var t in mw.MainTabControl.Items)
                    {
                        ((SukiSideMenuItem)t!).IsVisible = false;
                    }
                    mw.InfoTab.IsVisible = true;
                    mw.MainTabControl.SelectedItems.Clear();
                    mw.MainTabControl.SelectedItems.Add(mw.InfoTab);
                    mw.InfoBox.Text = "A run-time error has occured\n\n" + value[3..];
                    mw.InfoBox.WrapText = true;
                });
                StaticUtils.LiveLoadStatus = "";
            }).Start();
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            mw.LoadStatus.Text = value;
            mw.LoadProgress.IsIndeterminate = !(StaticUtils.LiveLoadStatus?.Contains('%') ?? false);
            mw.Loader.IsVisible = value != "";
            mw.DockPanel1.IsVisible = value == "";
            if (mw.LoadProgress.IsIndeterminate) return;
            mw.LoadProgress.Value = 100;
            try
            {
                mw.LoadProgress.Value =
                    int.Parse(StaticUtils.LiveLoadStatus!.Split(" (")[1].Split('%')[0].Split('.')[0]) / 100.0 * 28.0;
            }
            catch
            {
                mw.LoadProgress.Value = 0;
            }
        });
    }
}