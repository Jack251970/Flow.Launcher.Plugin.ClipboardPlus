using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using ClipboardPlus.Core;
using FluentIcons.WPF;
using FluentIcons.Common;

namespace ClipboardPlus.Panels;

public partial class PreviewPanel : UserControl
{
    private ClipboardData _clipboardData;
    private readonly PluginInitContext _context;
    private DirectoryInfo CacheDir { get; set; }
    private Action<ClipboardData>? DeleteOneRecord { get; set; }
    private Action<ClipboardData>? CopyRecord { get; set; }
    private Action<ClipboardData>? PinRecord { get; set; }
    private int OldScore { get; set; }
    private bool Ready { get; set; } = false;
    private const string WordsCountPrefix = "Words Count: ";
    private readonly SymbolIcon PinFluentIcon = new() { Symbol = Symbol.Pin };
    private readonly SymbolIcon UnpinFluentIcon = new() { Symbol = Symbol.PinOff };

    public PreviewPanel(
        ClipboardData clipboardData,
        PluginInitContext context,
        DirectoryInfo cacheDir,
        Action<ClipboardData> delAction,
        Action<ClipboardData> copyAction,
        Action<ClipboardData> pinAction
    )
    {
        _clipboardData = clipboardData;
        _context = context;
        CacheDir = cacheDir;
        DeleteOneRecord = delAction;
        CopyRecord = copyAction;
        PinRecord = pinAction;
        OldScore = clipboardData.Score;
        InitializeComponent();
        SetBtnIcon();
        SetContent();
        Ready = true;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public PreviewPanel()
    {
        _context = null!;
        CacheDir = null!;
        InitializeComponent();
        Ready = true;
    }

    private void SetBtnIcon()
    {
        BtnPin.Content = _clipboardData.Pined ? UnpinFluentIcon : PinFluentIcon;
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
        if (_clipboardData.Data is System.Drawing.Image img)
        {
            var im = img.ToBitmapImage();
            PreImage.Source = im;
        }
    }

    #region Image Viewer

    private void ImSaveAs_Click(object sender, RoutedEventArgs e)
    {
        Utils.SaveImageCache(_clipboardData, CacheDir);
    }

    #endregion

    #region Text Viewer

    private void TxtBoxPre_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()));
    }

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            TextBlockWordCount.Text = WordsCountPrefix + TxtBoxPre.Text.Length;
        }
    }

    #endregion

    #region Buttons

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        // if textbox is visible, it means the record is a text ot files, change the data to text
        if (TxtBoxPre.IsVisible)
            _clipboardData.Data = TxtBoxPre.Text;
        CopyRecord?.Invoke(_clipboardData);
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        _clipboardData.Pined = !_clipboardData.Pined;
        _clipboardData.Score = _clipboardData.Pined ? int.MaxValue : _clipboardData.InitScore;
        PinRecord?.Invoke(_clipboardData);
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        DeleteOneRecord?.Invoke(_clipboardData);
    }

    #endregion
}
