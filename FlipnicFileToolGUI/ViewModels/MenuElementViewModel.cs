using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.ViewModels;

public sealed class MenuElementViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Name of the layer/section
    /// </summary>
    public string? Layer { get; set; }
    
    /// <summary>
    /// Menu element object (describing the layout)
    /// </summary>
    public MenuElement MenuElement { get; init; }
    
    /// <summary>
    /// Position of the menu element as Avalonia thickness property
    /// </summary>
    public Thickness Pos => new(MenuElement.PosX, MenuElement.PosY, 0, 0);

    /// <summary>
    /// The image to display for this menu element
    /// </summary>
    public Bitmap? ImageSource { get; set; }

    /// <summary>
    /// Toggles the visibility of a menu element
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}