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

        // Preview panels
        PreviewTextTabItem.Content = new PreviewPanel(ClipboardPlus, GetRandomClipboardData(DataType.Text));
        // TODO: Test image.Save() method
        PreviewImageTabItem.Content = new PreviewPanel(ClipboardPlus, GetRandomClipboardData(DataType.Image));
        PreviewFilesTabItem.Content = new PreviewPanel(ClipboardPlus, GetRandomClipboardData(DataType.Files));
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var newCulture = ClipboardPlus.CultureInfo.Name == "en-US" ? "zh-CN" : "en-US";
        var newCultureInfo = new CultureInfo(newCulture);
        ClipboardPlus.OnCultureInfoChanged(newCultureInfo);
    }

    private ClipboardData GetRandomClipboardData(DataType type)
    {
        var rand = new Random();
        var data = new ClipboardData()
        {
            HashId = StringUtils.GetGuid(),
            Text = StringUtils.RandomString(10),
            DataType = type,
            Data = StringUtils.RandomString(10),
            SenderApp = StringUtils.RandomString(5) + ".exe",
            Title = StringUtils.RandomString(10),
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

    public static BitmapImage? LoadBitmapImageByPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }
            BitmapImage bi = new BitmapImage();
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    byte[] bytes = br.ReadBytes((int)stream.Length);
                    bi.BeginInit();
                    bi.StreamSource = new MemoryStream(bytes);
                    bi.EndInit();
                }
            }
            return bi;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
