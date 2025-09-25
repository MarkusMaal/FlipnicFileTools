using Avalonia;
using Avalonia.Controls;

namespace FlipnicFileToolGUI.Controls;

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

    public bool IsLightTheme
    {
        get => GetValue(IsLightThemeProperty);
        set
        {
            try
            {
                SetValue(IsLightThemeProperty, value);
            }
            catch
            {
                SetValue(IsLightThemeProperty, true);
            }
        }
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CircleDot, string>(nameof(Text), defaultValue: Design.IsDesignMode ? "Sample text\n\nThis text should only be displayed\nwhen designing this UserControl." : "undefined");
    public static readonly StyledProperty<bool> IsLightThemeProperty = AvaloniaProperty.Register<CircleDot, bool>(nameof(IsLightTheme), defaultValue: Design.IsDesignMode);
}