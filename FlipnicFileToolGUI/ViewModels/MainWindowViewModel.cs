using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.ViewModels;

public class MainWindowViewModel
{
    public Dictionary<string, Gimmick[]>? Gimmicks { get; set; }
    
    public ObservableCollection<VirtualFile>? VirtualFiles { get; set; }
    
    public ObservableCollection<SampleColl> Samples { get; set; }
    
    public List<MenuElementViewModel> _menu = [];

    public ObservableCollection<MenuElementViewModel> MenuElements { get; set; }
    
    public ObservableCollection<string> Controls { get; set; } = new(["L2", "R2", "L1", "R1", "Triangle", "Circle", "Cross", "Square", "Unk8", "Unk9", "UnkA", "UnkB", "DPadUp", "DPadRight", "DPadDown", "DPadLeft"]);

    public FpnSave SaveData { get; set; } = new FpnSave(new byte[0x2780]);
    public bool IsLightTheme { get; set; } = Design.IsDesignMode;
    public bool DevMode { get; set; }

    public static readonly StyledProperty<bool> DevModeProperty = AvaloniaProperty.Register<MainWindow, bool>(nameof(DevMode), defaultValue: true);
}