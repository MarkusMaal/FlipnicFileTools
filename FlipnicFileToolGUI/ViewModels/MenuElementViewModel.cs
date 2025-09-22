using Avalonia;
using Avalonia.Media.Imaging;
using FlipnicLib.Formats;

namespace FlipnicFileToolGUI.ViewModels;

public class MenuElementViewModel
{
    public string Layer { get; set; }
    public FpnMlb.MenuElement MenuElement { get; init; }
    
    public Thickness Pos => new(MenuElement.PosX, MenuElement.PosY, 0, 0);

    public Bitmap ImageSource { get; set; }

    public bool IsVisible { get; set; } = true;
}