using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using BigGustave;
using FlipnicLib;
using SukiUI;

namespace FlipnicFileToolGUI;

public abstract class Preferences
{
    private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "flipnic-file-tools.xml");
    
    public static void SavePreferences(bool lightTheme, string? msgFile)
    {
        if (Design.IsDesignMode) return;
        var saveData = new XDocument();
        var root = new XElement("FlipnicFileTools");
        root.SetAttributeValue("Version", StaticUtils.LibVersion);
        root.SetAttributeValue("Beta", StaticUtils.IsBeta);
        root.SetAttributeValue("Check", CalcSum(lightTheme, msgFile));
        root.Add(new XElement("IsLightTheme", lightTheme));
        root.Add(new XElement("MsgFile", msgFile));
        saveData.Add(root);
        if (Debugger.IsAttached) Console.WriteLine("Save preferences to: " + SavePath);
        try
        {
            saveData.Save(SavePath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or AccessViolationException)
        {
            Console.WriteLine("Failed to save preferences");
        }
    }

    private static string CalcSum(bool lightTheme, string? msgFile)
    {
        var mainSum = (Crc32
            .Calculate(Encoding.UTF8.GetBytes(lightTheme + msgFile)) ^ 0xFFFFFFFF)
            .ToString("X");
        var backupSum = (Crc32
                .Calculate(Encoding.UTF8.GetBytes(mainSum + lightTheme + msgFile)) ^ 0xFFFFFFFF)
            .ToString("X");
        return backupSum + mainSum;
    }

    public static void LoadPreferences(MainWindow mw)
    {
        if (!File.Exists(SavePath))
        {
            if (mw.FileName == null)
            {
                mw.InfoBox.Text += "\nUsing default settings";
            }
            return;
        }
        var xml = XDocument.Load(Path.Combine(SavePath));
        if (float.Parse(xml.Root!.Attribute("Version")!.Value, CultureInfo.GetCultureInfo("en-US")) >
            StaticUtils.LibVersion || bool.Parse(xml.Root!.Attribute("Beta")?.Value!) != StaticUtils.IsBeta)
        {
            if (mw.FileName == null)
            {
                mw.InfoBox.Text += "\nWarning: Incompatible preferences file, settings have been reset!";
            }
            return;
        }
        var testLight = xml.Root!.Element("IsLightTheme")!.Value == "true";
        var testMsg = xml.Root!.Element("MsgFile")!.Value;
        var realCrc = CalcSum(testLight, testMsg);
        var readCrc = xml.Root!.Attribute("Check")!.Value;
        if (realCrc != readCrc)
        {
            if (mw.FileName == null)
            {
                mw.InfoBox.Text += "\nWarning: Preferences file may be corrupt, settings have been reset!";
            }
            return;
        }
        if ((xml.Root!.Element("IsLightTheme")!.Value) == "true")
        {
            new Thread(() =>
            {
                Thread.Sleep(200); // IDK why it's necessary, but light mode won't apply if we don't include this delay
                while (!mw.IsLoaded) Thread.Sleep(100); // just in case
                Dispatcher.UIThread.Post(() =>
                {
                    mw.PalMenuItem_OnClick(null, null);

                    if (!Program.GpuAccel) return;
                    SukiTheme.GetInstance().ChangeBaseTheme(ThemeVariant.Light);
                    MainWindow.ApplyCustomTheme();
                });
            }).Start();

            mw.InfoBox.IsLightTheme = true; // apply you little sh...
        }
        StaticUtils.MsgFile = xml.Root!.Element("MsgFile")!.Value;
        if (!File.Exists(StaticUtils.MsgFile)) { StaticUtils.MsgFile = ""; }
        if (mw.FileName == null)
        {
            mw.InfoBox.Text += "\nLoaded preferences from: " + SavePath;
        }
    }
}