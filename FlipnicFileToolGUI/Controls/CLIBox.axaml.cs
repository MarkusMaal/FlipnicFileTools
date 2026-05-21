using Avalonia;
using Avalonia.Controls;

namespace FlipnicFileToolGUI.Controls;

public partial class CLIBox : UserControl
{
    public CLIBox()
    {
        InitializeComponent();
    }

    public bool WrapText
    {
        get => GetValue(WrapTextProperty);
        set => SetValue(WrapTextProperty, value);
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

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<CLIBox, string>(nameof(Text), defaultValue: Design.IsDesignMode ? "Sample text\n\nThis text should only be displayed\nwhen designing this UserControl." : "undefined");
    public static readonly StyledProperty<bool> IsLightThemeProperty = AvaloniaProperty.Register<CLIBox, bool>(nameof(IsLightTheme), defaultValue: Design.IsDesignMode);
    public static readonly StyledProperty<bool> WrapTextProperty = AvaloniaProperty.Register<CLIBox, bool>(nameof(WrapText), defaultValue: false);
}