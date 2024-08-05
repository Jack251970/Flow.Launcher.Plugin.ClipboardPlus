using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels;

public partial class PreviewPanel : UserControl
{
    #region Properties

    // Plugin context
    private readonly PluginInitContext Context = null!;

    // Clipboard data
    private ClipboardData ClipboardData;

    // Initial state
    private bool Ready { get; set; } = false;

    // Words count prefix
    private string WordsCountPrefix => Context?.API.GetTranslation("flowlauncher_plugin_clipboardplus_words_count_prefix") ?? "Words count: ";

    #endregion

    #region Constructors

    public PreviewPanel(PluginInitContext context, ClipboardData clipboardData)
    {
        Context = context;
        ClipboardData = clipboardData;
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
        InitializeComponent();
        Ready = true;
    }

    #endregion

    #region Setters

    public void SetContent()
    {
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
                SetImage();
                break;
            default:
                break;
        }
    }

    public void SetText(string s = "")
    {
        PreTxtBox.Clear();
        PreTxtBox.Text = string.IsNullOrWhiteSpace(s) ? ClipboardData.Text : s;
        PreSubTitle.Text = WordsCountPrefix + StringUtils.CountWords(PreTxtBox.Text);
    }

    public void SetImage()
    {
        PreImage.Visibility = Visibility.Visible;
        PreTxtBoxSv.Visibility = Visibility.Collapsed;
        PreSubTitle.Visibility = Visibility.Collapsed;
        if (ClipboardData.Data is System.Drawing.Image img)
        {
            var im = img.ToBitmapImage();
            PreImage.Source = im;
        }
    }

    #endregion

    #region Text Viewer

    private void PreTxtBox_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
    }

    #endregion

    #region Preview Subtitle

    private void PreTxtBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            PreSubTitle.Text = WordsCountPrefix + PreTxtBox.Text.Length;
        }
    }

    #endregion
}
