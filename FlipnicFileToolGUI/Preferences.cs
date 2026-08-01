using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

    public static readonly List<string> RecentFiles = [];
    
    public static void SavePreferences(bool lightTheme, string? msgFile)
    {
        if (Design.IsDesignMode) return;
        var saveData = new XDocument();
        var root = new XElement("FlipnicFileTools");
        root.SetAttributeValue("Version", StaticUtils.LibVersion);
        root.SetAttributeValue("Beta", StaticUtils.IsBeta);
        root.Add(new XElement("IsLightTheme", lightTheme));
        root.Add(new XElement("MsgFile", msgFile));
        var recents = new XElement("RecentFiles");
        foreach (var recent in RecentFiles)
        {
            recents.Add(new XElement("File", recent));
        }
        root.SetAttributeValue("Check", CalcSum(lightTheme, msgFile, RecentFiles.ToArray()));
        root.Add(recents);
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

    private static string CalcSum(bool lightTheme, string? msgFile, string[] recents)
    {
        var mainSum = (Crc32
            .Calculate(Encoding.UTF8.GetBytes(string.Join("-", recents) + lightTheme + msgFile)) ^ 0xFFFFFFFF)
            .ToString("X");
        var backupSum = (Crc32
                .Calculate(Encoding.UTF8.GetBytes(mainSum + string.Join("", recents) + lightTheme + msgFile)) ^ 0xFFFFFFFF)
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
        if (xml.Root!.Attribute("Version") == null || xml.Root!.Attribute("Beta") == null || xml.Root!.Attribute("Check") == null)
        {
            if (mw.FileName == null)
            {
                mw.InfoBox.Text += "\nWarning: Invalid XML markup, settings have been reset!";
            }
            return;
        }
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
        // for backwards compatibility with config files from previous versions
        var recents = xml.Root.Element("RecentFiles") != null ? xml.Root!.Element("RecentFiles")!.Elements().Select(p => p.Value).ToArray() : [];
        var realCrc = CalcSum(testLight, testMsg, recents);
        var readCrc = xml.Root!.Attribute("Check")!.Value;
        if (realCrc != readCrc)
        {
            if (mw.FileName == null)
            {
                mw.InfoBox.Text += "\nWarning: Preferences file may be corrupt, settings have been reset!";
            }
            return;
        }
        RecentFiles.Clear();
        RecentFiles.AddRange(recents.Where(p => File.Exists(p)));
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