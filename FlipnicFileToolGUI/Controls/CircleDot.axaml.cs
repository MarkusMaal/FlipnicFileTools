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
    public IImageBrushSource Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double IconWidth => this.Width * 0.75;
    
    public static readonly StyledProperty<IImageBrushSource> SourceProperty = AvaloniaProperty.Register<CircleDot, IImageBrushSource>(nameof(Source));
    
    public static readonly StyledProperty<Color> ColorAProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorA), defaultValue: Colors.MediumBlue);
    
    public static readonly StyledProperty<Color> ColorBProperty =
        AvaloniaProperty.Register<CircleDot, Color>(nameof(ColorB), defaultValue: Color.FromRgb(0,0,50));
}