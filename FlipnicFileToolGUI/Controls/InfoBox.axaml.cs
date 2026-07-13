using Avalonia;
using Avalonia.Controls;

namespace FlipnicFileToolGUI.Controls;

public partial class InfoBox : UserControl
{
    public InfoBox()
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

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<InfoBox, string>(nameof(Text), defaultValue: Design.IsDesignMode ? "Sample text\n\nThis text should only be displayed\nwhen designing this UserControl." : "undefined");
    public static readonly StyledProperty<bool> IsLightThemeProperty = AvaloniaProperty.Register<InfoBox, bool>(nameof(IsLightTheme), defaultValue: Design.IsDesignMode);
    public static readonly StyledProperty<bool> WrapTextProperty = AvaloniaProperty.Register<InfoBox, bool>(nameof(WrapText), defaultValue: false);
}