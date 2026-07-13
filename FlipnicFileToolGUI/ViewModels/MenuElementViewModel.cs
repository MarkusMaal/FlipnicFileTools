using System.ComponentModel;
using Avalonia.Media.Imaging;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.ViewModels;

public sealed class MenuElementViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Menu element object (describing the layout)
    /// </summary>
    public MenuElement? MenuElement { get; init; }

    /// <summary>
    /// The image to display for this menu element
    /// </summary>
    public Bitmap? ImageSource { get; set; }

    /// <summary>
    /// Toggles the visibility of a menu element
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;
}