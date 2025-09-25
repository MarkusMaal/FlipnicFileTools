using Avalonia;
using Avalonia.Media.Imaging;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.ViewModels;

public class MenuElementViewModel
{
    /// <summary>
    /// Name of the layer/section
    /// </summary>
    public string Layer { get; set; }
    
    /// <summary>
    /// Menu element object (describing the layout)
    /// </summary>
    public FpnMlb.MenuElement MenuElement { get; init; }
    
    /// <summary>
    /// Position of the menu element as Avalonia thickness property
    /// </summary>
    public Thickness Pos => new(MenuElement.PosX, MenuElement.PosY, 0, 0);

    /// <summary>
    /// The image to display for this menu element
    /// </summary>
    public Bitmap ImageSource { get; set; }

    /// <summary>
    /// Toggles the visibility of a menu element
    /// </summary>
    public bool IsVisible { get; set; } = true;
}