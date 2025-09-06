using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace FlipnicFileToolGUI;

public partial class MenuMockup : UserControl
{
    public MenuMockup()
    {
        InitializeComponent();
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
            if (menuElement.MenuElement.Texture == ((string?)((CheckBox?)sender)?.Content ?? ""))
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
        var topLevel = TopLevel.GetTopLevel(this);
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [Filters.PngFile]
        });

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
        bitmap.Save(Uri.UnescapeDataString(file.Path.AbsolutePath));
        ((MainWindow?)topLevel)?.ShowDialog("Flipnic file tools", "File was saved successfully!", NotificationType.Success);

    }
}