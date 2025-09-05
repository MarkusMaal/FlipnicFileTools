using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FlipnicFileToolGUI;

public partial class CircleDot : UserControl
{
    
    public CircleDot()
    {
        InitializeComponent();
    }
    
    public Color ColorA
    {
        get => GetValue(ColorAProperty);
        set => SetValue(ColorAProperty, value);
    }
    public Color ColorB
    {
        get => GetValue(ColorBProperty);
        set => SetValue(ColorBProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CircleDot, string>(nameof(Text), defaultValue: "");
    
    public static readonly StyledProperty<Color> ColorAProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorA), defaultValue: Colors.Black);
    
    public static readonly StyledProperty<Color> ColorBProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorB), defaultValue: Colors.Black);
}