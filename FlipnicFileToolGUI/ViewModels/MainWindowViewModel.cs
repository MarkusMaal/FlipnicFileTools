using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
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

    /// <summary>
    /// App > Enable developer features?
    /// </summary>
    //public bool DevMode { get; set; };

    //public static readonly StyledProperty<bool> DevModeProperty = AvaloniaProperty.Register<MainWindow, bool>(nameof(DevMode), defaultValue: false);
    public ObservableCollection<MenuElementViewModel>? MenuElements { get; set; }
    
    public bool CanOpenImhex
    {
        get;
        set;
    }
    
    /// <summary>
    /// List of controls to be displayed on a Combobox
    /// </summary>
    public ObservableCollection<string> Controls { get; set; } = new(["L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square", "Unk8", "Unk9", "UnkA", "UnkB", "DPadUp", "DPadRight", "DPadDown", "DPadLeft"]);
}