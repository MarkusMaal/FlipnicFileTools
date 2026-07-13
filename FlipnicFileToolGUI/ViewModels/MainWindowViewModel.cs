using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
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

    public event PropertyChangedEventHandler? PropertyChanged;
}