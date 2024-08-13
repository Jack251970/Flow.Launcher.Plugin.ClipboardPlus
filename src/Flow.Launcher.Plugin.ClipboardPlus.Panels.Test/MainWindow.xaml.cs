using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ClipboardPlus ClipboardPlus = new();

    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private readonly BitmapImage _defaultImage = new(new Uri(Path.Combine(_baseDirectory, _defaultIconPath), UriKind.Absolute));

    private readonly static string _imageSavePath = @"D:\clipboard.png";

    public MainWindow()
    {
        InitializeComponent();

        // Settings panel
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var button = new Button() { Content = "Click to invoke OnCultureInfoChanged event" };
        button.Click += Button_Click;
        Grid.SetRow(button, 0);
        var settingsPanel = new SettingsPanel(ClipboardPlus);
        Grid.SetRow(settingsPanel, 1);
        grid.Children.Add(button);
        grid.Children.Add(settingsPanel);
        PreviewSettingsTabItem.Content = grid;

        // Preview text panel
        var grid1 = new Grid();
        grid1.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid1.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var dataText = GetRandomClipboardData(DataType.Text);
        var previewPanel1 = new PreviewPanel(ClipboardPlus, dataText);
        Grid.SetRow(previewPanel1, 0);
        var label1 = new Label() { Content = $"Title: {dataText.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataText.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataText.GetSubtitle(CultureInfo.CurrentCulture)}" };
        Grid.SetRow(label1, 1);
        grid1.Children.Add(previewPanel1);
        grid1.Children.Add(label1);
        PreviewTextTabItem.Content = grid1;

        // Preview image panel
        var grid2 = new Grid();
        grid2.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid2.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid2.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var button2 = new Button() { Content = $"Click to save image to {_imageSavePath}" };
        button2.Click += Button_Click2;
        Grid.SetRow(button2, 0);
        var dataImage = GetRandomClipboardData(DataType.Image);
        var previewPanel2 = new PreviewPanel(ClipboardPlus, dataImage);
        Grid.SetRow(previewPanel2, 1);
        var label2 = new Label() { Content = $"Title: {dataImage.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataImage.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataImage.GetSubtitle(CultureInfo.CurrentCulture)}" };
        Grid.SetRow(label2, 2);
        grid2.Children.Add(button2);
        grid2.Children.Add(previewPanel2);
        grid2.Children.Add(label2);
        PreviewImageTabItem.Content = grid2;

        // Preview files panel
        var grid3 = new Grid();
        grid3.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid3.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var dataFiles = GetRandomClipboardData(DataType.Files);
        var previewPanel3 = new PreviewPanel(ClipboardPlus, dataFiles);
        Grid.SetRow(previewPanel3, 0);
        var label3 = new Label() { Content = $"Title: {dataFiles.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataFiles.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataFiles.GetSubtitle(CultureInfo.CurrentCulture)}" };
        Grid.SetRow(label3, 1);
        grid3.Children.Add(previewPanel3);
        grid3.Children.Add(label3);
        PreviewFilesTabItem.Content = grid3;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var newCulture = ClipboardPlus.CultureInfo.Name == "en-US" ? "zh-CN" : "en-US";
        var newCultureInfo = new CultureInfo(newCulture);
        ClipboardPlus.OnCultureInfoChanged(newCultureInfo);
    }

    private void Button_Click2(object sender, RoutedEventArgs e)
    {
        _defaultImage.Save(_imageSavePath);
    }

    private ClipboardData GetRandomClipboardData(DataType type)
    {
        var rand = new Random();
        var data = new ClipboardData()
        {
            HashId = StringUtils.GetGuid(),
            DataType = type,
            Data = StringUtils.RandomString(10),
            SenderApp = StringUtils.RandomString(5) + ".exe",
            CachedImagePath = _defaultIconPath,
            Score = rand.Next(1000),
            InitScore = rand.Next(1000),
            Pinned = false,
            CreateTime = DateTime.Now,
        };
        if (data.DataType == DataType.Image)
        {
            data.Data = _defaultImage;
        }
        else if (data.DataType == DataType.Files)
        {
            data.Data = new string[] { StringUtils.RandomString(10), StringUtils.RandomString(10) };
        }
        return data;
    }
}
