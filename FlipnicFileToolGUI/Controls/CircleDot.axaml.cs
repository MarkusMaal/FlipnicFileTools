using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FlipnicFileToolGUI.Controls;

public partial class CircleDot : UserControl
{
    
    public CircleDot()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// First color of the icon gradient
    /// </summary>
    public Color ColorA
    {
        get => GetValue(ColorAProperty);
        set => SetValue(ColorAProperty, value);
    }
    
    /// <summary>
    /// Second color of the icon gradient
    /// </summary>
    public Color ColorB
    {
        get => GetValue(ColorBProperty);
        set => SetValue(ColorBProperty, value);
    }

    /// <summary>
    /// Text content of the icon
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CircleDot, string>(nameof(Text), defaultValue: "?");
    
    public static readonly StyledProperty<Color> ColorAProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorA), defaultValue: Colors.MediumBlue);
    
    public static readonly StyledProperty<Color> ColorBProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorB), defaultValue: Color.FromRgb(0,0,50));
}