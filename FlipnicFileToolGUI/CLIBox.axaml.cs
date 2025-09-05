using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FlipnicFileToolGUI;

public partial class CLIBox : UserControl
{
    public CLIBox()
    {
        InitializeComponent();
    }
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CircleDot, string>(nameof(Text), defaultValue: "");
}