using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using FlipnicLib.Types;

namespace FlipnicFileToolGUI.Controls;

public partial class MenuMockup : UserControl
{
    public MenuMockup()
    {
        InitializeComponent();
        if (!Design.IsDesignMode) return;
        var menuEls = new List<MenuElementViewModel>();
        for (var i = 0; i < 10; i++)
        {
            menuEls.Add(new MenuElementViewModel
            {
                ImageSource = new Bitmap(StaticUtils.GenerateCheckerboardPng(320, 240)),
                IsVisible = true,
                MenuElement = new MenuElement(new byte[0x60], "Dummy " + i)
            });
        }

        MockupDisplay.Background = new ImageBrush(new Bitmap(StaticUtils.GenerateCheckerboardPng(320, 240)))
        {
            Stretch = Stretch.Fill
        };
        MenuElementSource = new ObservableCollection<MenuElementViewModel>(menuEls);
    }
    public ObservableCollection<MenuElementViewModel> MenuElementSource
    {
        get => GetValue(MenuElementSourceProperty);
        set => SetValue(MenuElementSourceProperty, value);
    }
    public Stretch ViewboxStretch
    {
        get => GetValue(ViewboxStretchProperty);
        set => SetValue(ViewboxStretchProperty, value);
    }

    public new int Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public new int Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }
    
    public static readonly StyledProperty<ObservableCollection<MenuElementViewModel>> MenuElementSourceProperty = AvaloniaProperty.Register<MenuMockup, ObservableCollection<MenuElementViewModel>>(nameof(MenuElementSource));
    public new static readonly StyledProperty<int> WidthProperty = AvaloniaProperty.Register<MenuMockup, int>(nameof(Width));
    public new static readonly StyledProperty<int> HeightProperty = AvaloniaProperty.Register<MenuMockup, int>(nameof(Height));
    public static readonly StyledProperty<Stretch> ViewboxStretchProperty = AvaloniaProperty.Register<MenuMockup, Stretch>(nameof(ViewboxStretch));

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb) return;
        List<MenuElementViewModel> menuElements = [];
        var chk = false;
        foreach (var menuElement in MenuElementSource)
        {
            if (menuElement.MenuElement?.ToString() == (cb?.Content?.ToString() ?? ""))
            {
                menuElement.IsVisible = cb?.IsChecked ?? false;
                chk = menuElement.IsVisible;
            }
            menuElements.Add(menuElement);
        }
        MenuElementSource = new ObservableCollection<MenuElementViewModel>(menuElements);
        DataContext = this;
        cb?.IsChecked = chk;
        // sometimes, the interpolation mode isn't re-applied correctly when checkboxes are toggled, so we need to do this
        new Thread(() =>
        {
            Thread.Sleep(100);
            Dispatcher.UIThread.Post(() =>
            {
                SetInterpolationMode(PixelatedRadioButton.IsChecked ?? false ? BitmapInterpolationMode.None : BitmapInterpolationMode.HighQuality);
            });
        }).Start();
    }

    private async void SaveAsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await FileHelpers.SaveFile(this, [Filters.PngFile]);
            if (file is null) return;
        
            var scTarget = PreviewBox;
            if (scTarget is null) return;
            var backupW = scTarget.Width;
            var backupH = scTarget.Height;
            scTarget.Width = 640;
            scTarget.Height = 480;
            var pixelSize = new PixelSize(640, 480);
            var size = new Size(640, 480);

            using RenderTargetBitmap bitmap = new(pixelSize);
            scTarget.Measure(size);
            scTarget.Arrange(new Rect(size));
            bitmap.Render(scTarget);
            bitmap.Save(Uri.UnescapeDataString(file), PngBitmapEncoderOptions.Default);
            scTarget.Width = backupW;
            scTarget.Height = backupH;
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "Error: " + ex.Message, NotificationType.Error);
        }
    }

    private void SetInterpolationMode(BitmapInterpolationMode interpolationMode)
    {
        foreach (var m in MockupDisplay.Presenter?.GetVisualChildren().First().GetVisualChildren() ?? [])
        {
            if (!m.GetVisualChildren().Any()) continue;
            if (m.GetVisualChildren().First() is not Image im) continue;
            RenderOptions.SetBitmapInterpolationMode(im, interpolationMode);
        }
    }

    private void RadioChecksChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb) return;
        if (!(rb.IsChecked ?? false)) return;
        if (rb.Content == null) return;
        LinearRadioButton.IsEnabled = (string)(rb.Content ?? "") != "Original size";
        PixelatedRadioButton.IsEnabled = LinearRadioButton.IsEnabled;
        switch (rb.Content)
        {
            case "Linear":
                SetInterpolationMode(BitmapInterpolationMode.HighQuality);
                break;
            case "Pixelated":
                SetInterpolationMode(BitmapInterpolationMode.None);
                break;
            case "Stretch":
                ViewboxStretch = Stretch.Fill;
                break;
            case "Fit":
                ViewboxStretch = Stretch.Uniform;
                break;
            case "Fill":
                ViewboxStretch = Stretch.UniformToFill;
                break;
            case "Original size":
                ViewboxStretch = Stretch.None;
                break;
        }
    }

    private void DataContextChange(object? sender, EventArgs e)
    {
        SetInterpolationMode(PixelatedRadioButton.IsChecked ?? false ? BitmapInterpolationMode.None : BitmapInterpolationMode.HighQuality);
    }

    private void InvertSelection(object? sender, RoutedEventArgs e)
    {
        CheckBox? checkBox = null;
        foreach (var m in TextureToggles.Presenter?.GetVisualChildren().First().GetVisualChildren() ?? [])
        {
            if (!m.GetVisualChildren().Any()) continue;
            if (m.GetVisualChildren().First() is not CheckBox cb) continue;
            cb.IsChecked = !cb.IsChecked;
            checkBox = cb;
        }
        ToggleButton_OnIsCheckedChanged(checkBox, e);
    }

    private void NextSection(object? sender, RoutedEventArgs e)
    {
        CheckBox? checkBox = null;
        string? targetSection = null;
        var startIdx = 0;

        foreach (var (i, m) in (TextureToggles.Presenter?.GetVisualChildren().First().GetVisualChildren() ?? []).Index())
        {
            if (!m.GetVisualChildren().Any()) continue;
            if (m.GetVisualChildren().First() is not CheckBox cb) continue;
            if (cb.IsChecked != true) continue;
            startIdx = i;
            break;
        }

        foreach (var m in TextureToggles.Presenter?.GetVisualChildren().First().GetVisualChildren().Skip(startIdx) ?? [])
        {
            if (!m.GetVisualChildren().Any()) continue;
            if (m.GetVisualChildren().First() is not CheckBox cb) continue;
            var label = (cb.Content?.ToString() ?? "").Split(" | ")[0];
            switch (targetSection)
            {
                case null when (cb.IsChecked ?? false):
                    cb.IsChecked = false;
                    break;
                case null when !(cb.IsChecked ?? false):
                    cb.IsChecked = true;
                    targetSection = (cb.Content?.ToString() ?? "").Split(" | ")[0];
                    break;
            }
            if (label == targetSection)
            {
                cb.IsChecked = true;
            } else if (targetSection != null)
            {
                cb.IsChecked = false;
            }
            checkBox = cb;
        }
        ToggleButton_OnIsCheckedChanged(checkBox, e);
    }
}