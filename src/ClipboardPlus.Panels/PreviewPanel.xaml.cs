using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flow.Launcher.Plugin;
using ClipboardPlus.Core;
using Material.Icons;
using Image = System.Drawing.Image;

namespace ClipboardPlus.Panels;

public partial class PreviewPanel : UserControl
{
    private ClipboardData _clipboardData;
    private PluginInitContext _context;
    private DirectoryInfo CacheDir { get; set; }
    private Action<ClipboardData> DeleteOneRecord { get; set; }
    private Action<ClipboardData> CopyRecord { get; set; }
    private Action<ClipboardData> PinRecord { get; set; }
    private int OldScore { get; set; }
    private bool Ready { get; set; } = false;
    private const string WordsCountPrefix = "Words Count: ";
    public string IconColor { get; set; } = CbColors.Blue500;
    public string PathCopy { get; set; } =
        MaterialIconDataProvider.GetData(MaterialIconKind.ContentCopy);
    public string PathPin { get; set; } = MaterialIconDataProvider.GetData(MaterialIconKind.Pin);
    public string PathPinOff { get; set; } =
        MaterialIconDataProvider.GetData(MaterialIconKind.PinOff);
    public string PathDelete { get; set; } =
        MaterialIconDataProvider.GetData(MaterialIconKind.DeleteForever);
    public string PathSave { get; set; } = MaterialIconDataProvider.GetData(MaterialIconKind.File);

    public PreviewPanel()
    {
        InitializeComponent();
        Ready = true;
    }

    public PreviewPanel(
        ClipboardData clipboardData,
        PluginInitContext context,
        DirectoryInfo cacheDir,
        Action<ClipboardData> delAction,
        Action<ClipboardData> copyAction,
        Action<ClipboardData> pinAction,
        string iconColor = CbColors.Blue500
    )
    {
        _clipboardData = clipboardData;
        _context = context;
        CacheDir = cacheDir;
        DeleteOneRecord = delAction;
        CopyRecord = copyAction;
        PinRecord = pinAction;
        OldScore = clipboardData.Score;
        IconColor = iconColor;
        InitializeComponent();

        SetContent();
        SetBtnIcon();
        Ready = true;
    }

    public void SetContent()
    {
        TxtBoxPre.Visibility = Visibility.Visible;
        PreImage.Visibility = Visibility.Hidden;
        switch (_clipboardData.Type)
        {
            case CbContentType.Text:
                SetText();
                break;
            case CbContentType.Files:
                var ss = _clipboardData.Data as string[] ?? Array.Empty<string>();
                var s = string.Join('\n', ss);
                SetText(s);
                break;
            case CbContentType.Image:
                TxtBoxPre.Visibility = Visibility.Hidden;
                PreImage.Visibility = Visibility.Visible;
                SetImage();
                break;
            case CbContentType.Other:
            default:
                break;
        }
    }

    public void SetText(string s = "")
    {
        TxtBoxPre.Clear();
        TxtBoxPre.Text = string.IsNullOrWhiteSpace(s) ? _clipboardData.Text : s;
        TextBlockWordCount.Text = WordsCountPrefix + Utils.CountWords(TxtBoxPre.Text);
    }

    public void SetImage()
    {
        if (_clipboardData.Data is not Image img)
            return;
        var im = img.ToBitmapImage();
        PreImage.Source = im;
    }

    private void BtnCopy_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // if textbox is visible, it means the record is a text ot files, change the data to text
        if (TxtBoxPre.IsVisible)
            _clipboardData.Data = TxtBoxPre.Text;
        CopyRecord?.Invoke(_clipboardData);
    }

    private void BtnDelete_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DeleteOneRecord?.Invoke(_clipboardData);
    }

    private void SetBtnIcon()
    {
        BtnPin.Content = FindResource(_clipboardData.Pined ? "PathPinOff" : "PathPin");
    }

    private void TxtBoxPre_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()));
    }

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
            TextBlockWordCount.Text = WordsCountPrefix + TxtBoxPre.Text.Length;
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        _clipboardData.Pined = !_clipboardData.Pined;
        _clipboardData.Score = _clipboardData.Pined ? int.MaxValue : _clipboardData.InitScore;
        PinRecord?.Invoke(_clipboardData);
    }

    private void ImSaveAs_Click(object sender, RoutedEventArgs e)
    {
        Utils.SaveImageCache(_clipboardData, CacheDir);
    }
}
