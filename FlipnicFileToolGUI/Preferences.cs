using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Avalonia.Controls;
using FlipnicLib;

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

    public static void LoadPreferences(MainWindow mw)
    {
        if (!File.Exists(SavePath)) return;
        var xml = XDocument.Load(Path.Combine(SavePath));
        if (float.Parse(xml.Root!.Attribute("Version")!.Value, CultureInfo.GetCultureInfo("en-US")) >
            StaticUtils.LibVersion || bool.Parse(xml.Root!.Attribute("Beta")?.Value!) != StaticUtils.IsBeta)
        {
            Console.WriteLine("Incompatible preferences file, settings will be reset.");
            return;
        }
        if ((xml.Root!.Element("IsLightTheme")!.Value) == "true") mw.PalMenuItem_OnClick(null, null);
        StaticUtils.MsgFile = xml.Root!.Element("MsgFile")!.Value;
        if (!File.Exists(StaticUtils.MsgFile)) { StaticUtils.MsgFile = ""; }
    }
}