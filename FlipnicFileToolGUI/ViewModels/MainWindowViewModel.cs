using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.ViewModels;

public class MainWindowViewModel
{
    /// <summary>
    /// SST > Gimmicks
    /// </summary>
    public Dictionary<string, Gimmick[]>? Gimmicks { get; set; }

    /// <summary>
    /// SST > Gimmicks (current selection)
    /// </summary>
    public Gimmick[] SelectedGimmick { get; set; } = [];
    
    /// <summary>
    /// BIN > /embedded file/
    /// </summary>
    public ObservableCollection<VirtualFile>? VirtualFiles { get; set; }
    
    /// <summary>
    /// BD > Sample (list)
    /// </summary>
    public ObservableCollection<SampleColl>? Samples { get; set; }
    
    /// <summary>
    /// MLB > Menu section > Menu element (list)
    /// </summary>
    public readonly List<MenuElementViewModel> Menu = [];

    /// <summary>
    /// Save data
    /// </summary>
    public FpnSave SaveData { get; set; } = new(new byte[0x2780]);
    
    /// <summary>
    /// App > Is light mode enabled?
    /// </summary>
    public bool IsLightTheme { get; set; } = Design.IsDesignMode;

    public bool MultipleWindowsOpen
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime al) return false;
            return al.Windows.Count > 1;
        }
    }
}