using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
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

    private readonly IClipboardMonitor ClipboardMonitorWPF = new ClipboardMonitorW() { ObserveLastEntry = false };

    private readonly IClipboardMonitor ClipboardMonitorWin = new ClipboardMonitorWin() { ObserveLastEntry = false };

    private readonly List<ClipboardData> ClipboardDatas = new();

    private readonly WindowsClipboardHelper Helper = new();

    private List<ClipboardData> RecordList = new();

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
            DataType.PlainText => "Hello, world!",
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
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
            CachedImagePath = string.Empty,
            Pinned = pinned,
            Saved = false,
        };
        if (type == DataType.RichText)
        {
            data.PlainText = "DataType.Files";
        }
        ClipboardDatas.Add(data);
        return data;
    }

    #endregion

    #region Clipboard Monitor

    private DateTime now;

    private async void OnClipboardChangeW(object? sender, ClipboardChangedEventArgs e)
    {
        if (e.Content is null || e.DataType == DataType.Other || sender is not IClipboardMonitor clipboardMonitor)
        {
            return;
        }

        // init clipboard data
        now = DateTime.Now;
        var clipboardData = new ClipboardData(e.Content, e.DataType, true)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = e.SourceApplication.Name,
            InitScore = 1,
            CreateTime = now,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
            CachedImagePath = string.Empty,
            Pinned = false,
            Saved = false,
        };
        if (e.DataType == DataType.RichText)
        {
            clipboardData.PlainText = clipboardMonitor.ClipboardText;
        }

        Dispatcher.Invoke(() =>
        {
            TextBlock1.Text = $"Count: {_count}\n" +
                $"ClipboardChangedEventArgs\n" +
                $"DataType: {e.DataType}\n" +
                $"SourceApplication: {e.SourceApplication.Name}\n" +
                $"Content: {e.Content}";
            TextBlock2.Text = $"ClipboardMonitor\n" +
                $"ClipboardText: {ClipboardMonitorWPF.ClipboardText}\n" +
                $"ClipboardRtfText: {ClipboardMonitorWPF.ClipboardRtfText}\n" +
                $"ClipboardFiles: {ClipboardMonitorWPF.ClipboardFiles}\n" +
                $"ClipboardImage: {ClipboardMonitorWPF.ClipboardImage}";
            TextBlock3.Text = $"ClipboardData\n" +
                $"DataMd5: {clipboardData.DataMd5}\n" +
                $"DataToString: {clipboardData.DataToString(false)}\n" +
                $"DataToString(Encrypted): {clipboardData.DataToString(true)}\n" +
                $"Title: {clipboardData.GetTitle(CultureInfo.CurrentCulture)}\n" +
                $"Subtitle: {clipboardData.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
                $"Text: {clipboardData.GetText(CultureInfo.CurrentCulture)}";

            TextBox.Text = clipboardMonitor.ClipboardText;
        });

        RecordList.Add(clipboardData);

        await ClipboardPlus.Database.AddOneRecordAsync(clipboardData, true);

        _count++;
    }

    private async void OnClipboardChangedWin(object? sender, ClipboardChangedEventArgs e)
    {
        if (e.Content is null || e.DataType == DataType.Other || sender is not IClipboardMonitor clipboardMonitor)
        {
            return;
        }

        await Task.Delay(900);

        // init clipboard data
        var clipboardData = new ClipboardData(e.Content, e.DataType, true)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = e.SourceApplication.Name,
            InitScore = 1,
            CreateTime = now,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
            CachedImagePath = string.Empty,
            Pinned = false,
            Saved = false
        };
        if (e.DataType == DataType.RichText)
        {
            clipboardData.PlainText = clipboardMonitor.ClipboardText;
        }

        var TextBlock1Text = $"Count: {_count - 1}\n" +
                $"ClipboardChangedEventArgs\n" +
                $"DataType: {e.DataType}\n" +
                $"SourceApplication: {e.SourceApplication.Name}\n" +
                $"Content: {e.Content}";
        var TextBlock2Text = $"ClipboardMonitor\n" +
            $"ClipboardText: {ClipboardMonitorWPF.ClipboardText}\n" +
            $"ClipboardRtfText: {ClipboardMonitorWPF.ClipboardRtfText}\n" +
            $"ClipboardFiles: {ClipboardMonitorWPF.ClipboardFiles}\n" +
            $"ClipboardImage: {ClipboardMonitorWPF.ClipboardImage}";
        var TextBlock3Text = $"ClipboardData\n" +
            $"DataMd5: {clipboardData.DataMd5}\n" +
            $"DataToString: {clipboardData.DataToString(false)}\n" +
            $"DataToString(Encrypted): {clipboardData.DataToString(true)}\n" +
            $"Title: {clipboardData.GetTitle(CultureInfo.CurrentCulture)}\n" +
            $"Subtitle: {clipboardData.GetSubtitle(CultureInfo.CurrentCulture)}\n" +
            $"Text: {clipboardData.GetText(CultureInfo.CurrentCulture)}";

        var TextBoxText = clipboardMonitor.ClipboardText;

        var right1 = TextBlock1Text == TextBlock1.Text;
        var right2 = TextBlock2Text == TextBlock2.Text;
        var right3 = TextBlock3Text == TextBlock3.Text;
        var right4 = TextBoxText == TextBox.Text;

        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(clipboardMonitor.ClipboardRtfText))
            {
                RichTextBox.SetPlainText(clipboardMonitor.ClipboardText);
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

        var allRight = right1 && right2 && right3 && right4;
        if (!allRight)
        {
            Debug.WriteLine(">>>>>>>>>>>>>>>>>>>>>ClipboardChangedWin: Not all right<<<<<<<<<<<<<<<<<<<<");
        }
    }

    #endregion

    #region Initialization

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

        // Preview plain text panel
        var grid1 = new Grid();
        grid1.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid1.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var dataText = GetRandomClipboardData(DataType.PlainText);
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
        PreviewPlainTextTabItem.Content = grid1;

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
        ClipboardMonitorWPF.ClipboardChanged += OnClipboardChangeW;
        ClipboardMonitorWPF.StartMonitoring();

        ClipboardMonitorWin.ClipboardChanged += OnClipboardChangedWin;
        ClipboardMonitorWin.StartMonitoring();

        Helper.SetClipboardPlus(ClipboardPlus);
        Helper.OnHistoryItemAdded += Helper_OnHistoryItemAdded;
        Helper.OnHistoryItemRemoved += Helper_OnHistoryItemRemoved;
        Helper.OnHistoryItemPinUpdated += Helper_OnHistoryItemPinUpdated;
    }

    private void Helper_OnHistoryItemAdded(object? sender, ClipboardData e)
    {
        Debug.WriteLine("Clipboard history item added: " + e.HashId);
    }

    private void Helper_OnHistoryItemRemoved(object? sender, string[] e)
    {
        Debug.WriteLine("Clipboard history item removed: " + string.Join(", ", e));
    }

    private void Helper_OnHistoryItemPinUpdated(object? sender, ClipboardData e)
    {
        Debug.WriteLine("Clipboard history item pin updated");
    }

    #endregion

    #region Events

    private void Window_Closed(object sender, EventArgs e)
    {
        ClipboardMonitorWPF.ClipboardChanged -= OnClipboardChangeW;
        ClipboardMonitorWPF.Dispose();
        ClipboardMonitorWin.ClipboardChanged -= OnClipboardChangedWin;
        ClipboardMonitorWin.Dispose();
        Helper.Dispose();
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
        ClipboardMonitorWPF.ClipboardChanged += OnClipboardChangeW;
        ClipboardMonitorWPF.StartMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Wait something copyed to clipboard...";
        TextBox.Text = "";
        RichTextBox.SetPlainText("");
    }

    private void Button_Click4(object sender, RoutedEventArgs e)
    {
        ClipboardMonitorWPF.PauseMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Clipboard monitor is paused.";
        TextBox.Text = "";
        RichTextBox.SetPlainText("");
    }

    private void Button_Click5(object sender, RoutedEventArgs e)
    {
        ClipboardMonitorWPF.ResumeMonitoring();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Wait something copyed to clipboard...";
        TextBox.Text = "";
        RichTextBox.SetPlainText("");
    }

    private void Button_Click6(object sender, RoutedEventArgs e)
    {
        ClipboardMonitorWPF.ClipboardChanged -= OnClipboardChangeW;
        ClipboardMonitorWPF.Dispose();
        TextBlock1.Text = "";
        TextBlock2.Text = "";
        TextBlock3.Text = "Clipboard monitor is Stopped.";
        TextBox.Text = "";
        RichTextBox.SetPlainText("");
        Button1.IsEnabled = false;
        Button2.IsEnabled = false;
        Button3.IsEnabled = false;
        Button4.IsEnabled = false;
    }

    #endregion
}
