using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using FlipnicFileToolGUI.Helpers;
using FlipnicFileToolGUI.ViewModels;
using FlipnicLib;
using FlipnicLib.Formats;

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
                Layer = "Example " + i,
                MenuElement = new FpnMlb.MenuElement(new byte[0x60], "Dummy " + i)
            });
        }
        MenuElementSource = new ObservableCollection<MenuElementViewModel>(menuEls);
    }
    public ObservableCollection<MenuElementViewModel> MenuElementSource
    {
        get => GetValue(MenuElementSourceProperty);
        set => SetValue(MenuElementSourceProperty, value);
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

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    { 
        List<MenuElementViewModel> menuElements = [];
        foreach (var menuElement in MenuElementSource)
        {
            if (menuElement.MenuElement.ToString() == (((CheckBox?)sender)?.Content?.ToString() ?? ""))
            {
                menuElement.IsVisible = ((CheckBox?)sender)?.IsChecked ?? false;
            }
            menuElements.Add(menuElement);
        }
        MenuElementSource = new ObservableCollection<MenuElementViewModel>(menuElements);
        DataContext = this;
    }

    private async void SaveAsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var file = await FileHelpers.SaveFile(this, [Filters.PngFile]);
        if (file is null) return;
        
        var scTarget = (Grid?)((MenuItem?)sender)?.Parent?.Parent?.Parent;
        if (scTarget is null) return;
        scTarget.Width = 640;
        scTarget.Height = 480;
        var pixelSize = new PixelSize(640, 480);
        var size = new Size(640, 480);

        using RenderTargetBitmap bitmap = new(pixelSize);
        scTarget.Measure(size);
        scTarget.Arrange(new Rect(size));
        bitmap.Render(scTarget);
        bitmap.Save(Uri.UnescapeDataString(file));
        ((MainWindow?)TopLevel.GetTopLevel(this))?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);

    }
}