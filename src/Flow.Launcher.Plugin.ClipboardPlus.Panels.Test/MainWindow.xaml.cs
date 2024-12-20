using System;
using System.Collections.Generic;
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
    #region Properties

    private readonly ClipboardPlus ClipboardPlus = new();

    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private readonly BitmapImage _defaultImage = new(new Uri(Path.Combine(_baseDirectory, _defaultIconPath), UriKind.Absolute));

    private readonly static string _imageSavePath = @"D:\clipboard.png";

    private readonly ClipboardMonitorW ClipboardMonitor = new() { ObserveLastEntry = false };

    private readonly List<ClipboardData> ClipboardDatas = new();

    private List<ClipboardData> RecordList = null!;

    private int _count = 0;

    #endregion

    #region Constructor

    public MainWindow()
    {
        InitializeComponent();
        InitializeWindow();
        InitializeDatabase();
        InitializeClipboardMonitor();
    }

    #endregion

    #region Clipboard Data

    private ClipboardData GetRandomClipboardData(DataType type)
    {
        var rand = new Random();
        object dataContent = type switch
        {
            DataType.UnicodeText => "Hello, world!",
            DataType.RichText => @"{\rtf\ansi{\fonttbl{\f0 Cascadia Mono;}}{\colortbl;\red43\green145\blue175;\red255\green255\blue255;\red0\green0\blue0;\red0\green0\blue255;}\f0 \fs19 \cf1 \cb2 \highlight2 DataType\cf3 .\cf4 Files}",
            DataType.Image => _defaultImage,
            DataType.Files => new string[] { "D:\\a.txt", "D:\\b.docx", "D:\\c" },
            _ => null!
        };
        var encrypt = rand.NextDouble() > 0.5;
        var pinned = rand.NextDouble() > 0.5;
        var data = new ClipboardData(dataContent, type, encrypt)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = "Flow.Launcher.exe",
            InitScore = 1,
            CreateTime = DateTime.Now,
            CachedImagePath = string.Empty,
            Pinned = pinned,
            Saved = false,
            UnicodeText = string.Empty,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5
        };
        if (type == DataType.RichText)
        {
            data.UnicodeText = "DataType.Files";
        }
        ClipboardDatas.Add(data);
        return data;
    }

    #endregion

    #region Clipboard Monitor

    private async void OnClipboardChangeW(object? sender, ClipboardMonitorW.ClipboardChangedEventArgs e)
    {
        if (e.Content is null || e.DataType == DataType.Other || sender is not ClipboardMonitorW clipboardMonitor)
        {
            return;
        }

        // init clipboard data
        var now = DateTime.Now;
        var clipboardData = new ClipboardData(e.Content, e.DataType, true)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = e.SourceApplication.Name,
            InitScore = 1,
            CreateTime = now,
            CachedImagePath = string.Empty,
            Pinned = false,
            Saved = false,
            UnicodeText = string.Empty,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5
        };
        if (e.DataType == DataType.RichText)
        {
            clipboardData.UnicodeText = clipboardMonitor.ClipboardText;
        }

        Dispatcher.Invoke(() =>
        {
            TextBlock1.Text = $"Count: {_count}\n" +
                $"ClipboardChangedEventArgs\n" +
                $"DataType: {e.DataType}\n" +
                $"SourceApplication: {e.SourceApplication.Name}\n" +
                $"Content: {e.Content}";
            TextBlock2.Text = $"ClipboardMonitor\n" +
                $"ClipboardText: {ClipboardMonitor.ClipboardText}\n" +
                $"ClipboardRtfText: {ClipboardMonitor.ClipboardRtfText}\n" +
                $"ClipboardFiles: {ClipboardMonitor.ClipboardFiles}\n" +
                $"ClipboardImage: {ClipboardMonitor.ClipboardImage}";
            TextBlock3.Text = $"ClipboardData\n" +
                $"DataMd5: {clipboardData.DataMd5}\n" +
                $"DataToString: {clipboardData.DataToString(false)}\n" +
                $"DataToString(Encrypted): {clipboardData.DataToString(true)}\n" +
                $"Title: {clipboardData.GetTitle(CultureInfo.CurrentCulture)}\n" +
                $"Subtitle: {clipboardData.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
                $"Text: {clipboardData.GetText(CultureInfo.CurrentCulture)}";

            TextBox.Text = clipboardMonitor.ClipboardText;
            if (string.IsNullOrEmpty(clipboardMonitor.ClipboardRtfText))
            {
                RichTextBox.SetUnicodeText(clipboardMonitor.ClipboardText);
            }
            else
            {
                RichTextBox.SetRichText(clipboardMonitor.ClipboardRtfText);
            }

            if (e.DataType is DataType.Image)
            {
                Image.Source = clipboardMonitor.ClipboardImage ?? _defaultImage;
            }
        });

        RecordList.Add(clipboardData);

        await ClipboardPlus.Database.AddOneRecordAsync(clipboardData, true);

        _count++;
    }

    private async void OnClipboardChange(object? sender, ClipboardMonitor.ClipboardChangedEventArgs e)
    {
        if (e.Content is null || e.DataType == DataType.Other)
        {
            return;
        }

        // init clipboard data
        var now = DateTime.Now;
        var clipboardData = new ClipboardData(e.Content, e.DataType, true)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = e.SourceApplication.Name,
            InitScore = 1,
            CreateTime = now,
            CachedImagePath = string.Empty,
            Pinned = false,
            Saved = false,
            UnicodeText = string.Empty,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5
        };
        if (e.DataType == DataType.RichText)
        {
            clipboardData.UnicodeText = ClipboardMonitor.ClipboardText;
        }

        TextBlock1.Text = $"Count: {_count}\n" +
            $"ClipboardChangedEventArgs\n" +
            $"DataType: {e.DataType}\n" +
            $"SourceApplication: {e.SourceApplication.Name}\n" +
            $"Content: {e.Content}";
        TextBlock2.Text = $"ClipboardMonitor\n" +
            $"ClipboardText: {ClipboardMonitor.ClipboardText}\n" +
            $"ClipboardRtfText: {ClipboardMonitor.ClipboardRtfText}\n" +
            $"ClipboardFiles: {ClipboardMonitor.ClipboardFiles}\n" +
            $"ClipboardImage: {ClipboardMonitor.ClipboardImage}";
        TextBlock3.Text = $"ClipboardData\n" +
            $"DataMd5: {clipboardData.DataMd5}\n" +
            $"DataToString: {clipboardData.DataToString(false)}\n" +
            $"DataToString(Encrypted): {clipboardData.DataToString(true)}\n" +
            $"Title: {clipboardData.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {clipboardData.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {clipboardData.GetText(CultureInfo.CurrentCulture)}";

        TextBox.Text = ClipboardMonitor.ClipboardText;
        if (string.IsNullOrEmpty(ClipboardMonitor.ClipboardRtfText))
        {
            RichTextBox.SetUnicodeText(ClipboardMonitor.ClipboardText);
        }
        else
        {
            RichTextBox.SetRichText(ClipboardMonitor.ClipboardRtfText);
        }

        RecordList.Add(clipboardData);

        await ClipboardPlus.Database.AddOneRecordAsync(clipboardData, true);

        _count++;
    }

    #endregion

    #region Events

    private void InitializeWindow()
    {
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

        // Preview unicode text panel
        var grid1 = new Grid();
        grid1.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid1.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var dataText = GetRandomClipboardData(DataType.UnicodeText);
        var previewPanel1 = new PreviewPanel(ClipboardPlus, dataText);
        Grid.SetRow(previewPanel1, 0);
        var label1 = new Label()
        {
            Content = $"Title: {dataText.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataText.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataText.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Encrypt: {dataText.EncryptData}\n"
        };
        Grid.SetRow(label1, 1);
        grid1.Children.Add(previewPanel1);
        grid1.Children.Add(label1);
        PreviewUnicodeTextTabItem.Content = grid1;

        // Preview rich text panel
        var grid4 = new Grid();
        grid4.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid4.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var dataRichText = GetRandomClipboardData(DataType.RichText);
        var previewPanel4 = new PreviewPanel(ClipboardPlus, dataRichText);
        Grid.SetRow(previewPanel4, 0);
        var label4 = new Label()
        {
            Content = $"Title: {dataRichText.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataRichText.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataRichText.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Encrypt: {dataRichText.EncryptData}"
        };
        Grid.SetRow(label4, 1);
        grid4.Children.Add(previewPanel4);
        grid4.Children.Add(label4);
        PreviewRichTextTabItem.Content = grid4;

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
        var label2 = new Label()
        {
            Content = $"Title: {dataImage.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataImage.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataImage.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Encrypt: {dataImage.EncryptData}"
        };
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
        var label3 = new Label()
        {
            Content = $"Title: {dataFiles.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {dataFiles.GetText(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {dataFiles.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Encrypt: {dataFiles.EncryptData}"
        };
        Grid.SetRow(label3, 1);
        grid3.Children.Add(previewPanel3);
        grid3.Children.Add(label3);
        PreviewFilesTabItem.Content = grid3;

        // Clipboard monitor
        TextBlock2.TextWrapping = TextWrapping.Wrap;
    }

    private async void InitializeDatabase()
    {
        await ClipboardPlus.Database.InitializeDatabaseAsync();
        RecordList = await ClipboardPlus.Database.GetAllRecordsAsync(true);
        var str = string.Empty;
        foreach (var record in RecordList)
        {
            str += $"{record.CreateTime}\n\n";
        }
        TextBoxDatabase.Text = str;
    }

    private void InitializeClipboardMonitor()
    {
        ClipboardMonitor.ClipboardChanged += OnClipboardChangeW;
        ClipboardMonitor.StartMonitoring();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        ClipboardMonitor.ClipboardChanged -= OnClipboardChangeW;
        ClipboardMonitor.Dispose();
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

    private void Button_Click3(object sender, RoutedEventArgs e)
    {
        ClipboardMonitor.ClipboardChanged += OnClipboardChangeW;
        ClipboardMonitor.StartMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Wait something copyed to clipboard...";
        TextBox.Text = "";
        RichTextBox.SetUnicodeText("");
    }

    private void Button_Click4(object sender, RoutedEventArgs e)
    {
        ClipboardMonitor.PauseMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Clipboard monitor is paused.";
        TextBox.Text = "";
        RichTextBox.SetUnicodeText("");
    }

    private void Button_Click5(object sender, RoutedEventArgs e)
    {
        ClipboardMonitor.ResumeMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Wait something copyed to clipboard...";
        TextBox.Text = "";
        RichTextBox.SetUnicodeText("");
    }

    private void Button_Click6(object sender, RoutedEventArgs e)
    {
        ClipboardMonitor.ClipboardChanged -= OnClipboardChangeW;
        ClipboardMonitor.Dispose();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Clipboard monitor is Stopped.";
        TextBox.Text = "";
        RichTextBox.SetUnicodeText("");
        Button1.IsEnabled = false;
        Button2.IsEnabled = false;
        Button3.IsEnabled = false;
        Button4.IsEnabled = false;
    }

    #endregion
}
