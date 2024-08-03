using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels;

public partial class PreviewPanel : UserControl
{
    private ClipboardData ClipboardData;
    private bool Ready { get; set; } = false;
    private readonly string WordsCountPrefix = string.Empty;

    public PreviewPanel(
        ClipboardData clipboardData,
        string wordsCountPrefix = "Words Count: "
    )
    {
        ClipboardData = clipboardData;
        WordsCountPrefix = wordsCountPrefix;
        InitializeComponent();
        SetContent();
        Ready = true;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public PreviewPanel()
    {
        WordsCountPrefix = "Words Count: ";
        InitializeComponent();
        Ready = true;
    }

    #region Setters

    public void SetContent()
    {
        TxtBoxPre.Visibility = Visibility.Visible;
        PreImage.Visibility = Visibility.Hidden;
        switch (ClipboardData.Type)
        {
            case CbContentType.Text:
                SetText();
                break;
            case CbContentType.Files:
                var ss = ClipboardData.Data as string[] ?? Array.Empty<string>();
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
        TxtBoxPre.Text = string.IsNullOrWhiteSpace(s) ? ClipboardData.Text : s;
        StatusBar.Text = WordsCountPrefix + StringUtils.CountWords(TxtBoxPre.Text);
    }

    public void SetImage()
    {
        if (ClipboardData.Data is System.Drawing.Image img)
        {
            var im = img.ToBitmapImage();
            PreImage.Source = im;
        }
    }

    #endregion

    #region Image Viewer

    private void ImSaveAs_Click(object sender, RoutedEventArgs e)
    {
        FileUtils.SaveImageCache(ClipboardData, PathHelpers.ImageCachePath);
    }

    #endregion

    #region Text Viewer

    private void TxtBoxPre_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
    }

    #endregion

    #region Status Bar

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            StatusBar.Text = WordsCountPrefix + TxtBoxPre.Text.Length;
        }
    }

    #endregion
}
