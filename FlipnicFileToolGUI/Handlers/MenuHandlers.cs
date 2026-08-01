using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using FlipnicFileToolGUI.Helpers;
using FlipnicLib;
using SukiUI;

namespace FlipnicFileToolGUI.Handlers;

public abstract class MenuHandlers
{
    private static readonly HttpClient Client = new();
    public static void DarkModeToggle(MainWindow mw)
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
        MainWindow.ApplyCustomTheme();
        
        var windows = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows;
        mw.GetViewModel().IsLightTheme = !mw.GetViewModel().IsLightTheme;
        foreach (var window in windows ?? [])
        {
            if (window is not MainWindow mainWindow) continue;
            mainWindow.InfoBox.IsLightTheme = mw.GetViewModel().IsLightTheme;
            mainWindow.EventBox.IsLightTheme = mw.GetViewModel().IsLightTheme;
        }
    }

    private static async void OpenFile(MainWindow mw, bool jaMsg = false)
    {
        try
        {
            if (Design.IsDesignMode) return;
            var file = await FileHelpers.OpenFile(mw, jaMsg
                ? [Filters.FpnMsg]
                :
                [
                    Filters.AllSupported,
                    Filters.BdFile,
                    Filters.BinFile,
                    Filters.SysCnf,
                    Filters.ColFile,
                    Filters.CsvFile,
                    Filters.DummyFile,
                    Filters.FpnFpc,
                    Filters.FpdFile,
                    Filters.FtlFile,
                    Filters.HdFile,
                    Filters.SaveIcon,
                    Filters.IpuFile,
                    Filters.IsoFile,
                    Filters.LayFile,
                    Filters.LitFile,
                    Filters.FpnLp4,
                    Filters.MidiFile,
                    Filters.FpnMlb,
                    Filters.FpnMsg,
                    Filters.SonyPss,
                    Filters.SccFile,
                    Filters.GameElf,
                    Filters.FpnSst,
                    Filters.SvagFile,
                    Filters.SonyTim2,
                    Filters.TxtFile,
                    Filters.VsdFile,
                    Filters.XmlFile
                ]);
            if (file == null) return;
            if (jaMsg)
            {
                StaticUtils.MsgFile = file;
                return;
            }
            if (!Preferences.RecentFiles.Any(p => p == file))
            {
                Preferences.RecentFiles.Add(file);
                if (Preferences.RecentFiles.Count > 5)
                {
                    Preferences.RecentFiles.RemoveAt(0);
                }
                mw.ReloadRecentMenu();
            }
            mw.FileName = file;
            FileHelpers.LoadFromData(new FileStream(file, FileMode.Open, FileAccess.Read), file[^3..], mw);
        }
        catch
        {
            // ignored
        }
    }
    
    

    public static void OpenMenuFromStr(string header, MainWindow mw)
    {
        switch (header)
        {
            case "Open":
                OpenFile(mw);
                break;
            case "Import JA.MSG":
                OpenFile(mw, true);
                break;
        }
    }

    public static void AltNormalMethodToggle(MainWindow mw, object? updatableMenuItem)
    {
        StaticUtils.AlternateNormals = !StaticUtils.AlternateNormals;
        var letter = StaticUtils.AlternateNormals ? "B" : "A";
        var newValue = $"Normal vectors decoding: Method {letter}";
        switch (updatableMenuItem)
        {
            case MenuItem mi:
                mi.Header = newValue;
                break;
            case NativeMenuItem nmi:
                nmi.Header = newValue;
                break;
        }
        if (mw.FileName is null) return;
        FileHelpers.LoadFromData(new FileStream(mw.FileName, FileMode.Open, FileAccess.Read), mw.FileName[^3..], mw);
    }
    
    

    public static void OpenUrl(string url, MainWindow mw)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                mw.ShowDialog("Error", $"Couldn't open URL. Please visit it manually:\n\n{url}", NotificationType.Error);
            }
        }
    }

    public static void OpenInImhex(MainWindow mw)
    {
        if (!File.Exists(mw.FileName)) return;
        new Thread(async void () =>
        {
            try
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo("imhex")
                    {
                        Arguments = "--open \"" + mw.FileName + "\""
                    }
                }.Start();
                Thread.Sleep(1000);
                var patternUrl = mw.FileName[^3..].ToUpper() switch
                {
                    "LP4" => "lp4.hexpat",
                    "BIN" => "binfile.hexpat",
                    "FPC" => "fpc.hexpat",
                    ".HD" => "hd.hexpat",
                    "IPU" => "ipu.hexpat",
                    "MSG" => "msg.hexpat",
                    "PSS" => "pss.hexpat",
                    "SCC" => "scc.hexpat",
                    "SST" => "sst.hexpat",
                    "TM2" => "tim2.hexpat",
                    "ICO" => "ico.hexpat",
                    "LIT" => "lit.hexpat",
                    "VSD" => "vsd.hexpat",
                    "COL" => "col.hexpat",
                    "FPD" => "fpd.hexpat",
                    "FTL" => "ftl.hexpat",
                    "MLB" => "mlb.hexpat",
                    "LAY" => "LAY.hexpat",
                    _ => ""
                };
                if (patternUrl == "") return;
                var tmpFile = Path.GetTempFileName();
                await DownloadFile(
                    $"https://raw.githubusercontent.com/MarkusMaal/FlipnicPatterns/refs/heads/main/patterns/{patternUrl}",
                    tmpFile);
                new Process()
                {
                    StartInfo = new ProcessStartInfo("imhex")
                    {
                        Arguments = "--pattern \"" + tmpFile + "\""
                    }
                }.Start();
            }
            catch
            {
                // ignore
            }
        }).Start();
        
    }
    

    private static async Task<byte[]?> GetUrlContent(string url)
    {
        using var result = await Client.GetAsync(url);
        return result.IsSuccessStatusCode ? await result.Content.ReadAsByteArrayAsync() : null;
    }
    
    private static async Task DownloadFile(string url, string pathToSave)
    {
        var content = await GetUrlContent(url);
        if (content != null)
        {
            await File.WriteAllBytesAsync($"{pathToSave}", content);
        }
    }
    
    

    public static void CloseOtherWindows()
    {
        var windows = ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.Windows;
        while (windows?.Count > 1)
        {
            foreach (var window in windows)
            {
                if (window.IsActive) continue;
                window.Close();
                break;
            }
        }
    }
}