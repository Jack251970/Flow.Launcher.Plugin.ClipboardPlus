using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels.Views;

public partial class PreviewPanel : UserControl
{
    #region Properties

    // Plugin context
    private readonly PluginInitContext? Context;

    // Clipboard data
    private ClipboardData ClipboardData;

    // Words count prefix
    private string WordsCountPrefix => Context.GetTranslation("flowlauncher_plugin_clipboardplus_words_count_prefix") ?? "Words count: ";

    #endregion

    #region Constructors

    public PreviewPanel(PluginInitContext context, ClipboardData clipboardData)
    {
        Context = context;
        ClipboardData = clipboardData;
        InitializeComponent();
        InitializePreTxtBox();
        SetContent();
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// </summary>
    public PreviewPanel()
    {
        InitializeComponent();
        InitializePreTxtBox();
    }

    #endregion

    #region Text Viewer

    private void InitializePreTxtBox()
    {
        PreTxtBox.GotFocus += PreTxtBox_GotFocus;
        PreTxtBox.TextChanged += PreTxtBox_TextChanged;
    }

    private void PreTxtBox_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
    }

    private void PreTxtBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PreSubTitle.Text = WordsCountPrefix + PreTxtBox.Text.Length;
    }

    #endregion

    #region Clipboard Data

    public void SetContent()
    {
        switch (ClipboardData.Type)
        {
            case DataType.Text:
                SetText(ClipboardData.Text);
                break;
            case DataType.Files:
                SetText(ClipboardData.Data as string[] ?? Array.Empty<string>());
                break;
            case DataType.Image:
                SetImage();
                break;
            default:
                break;
        }
    }

    public void SetText(string text)
    {
        PreTxtBox.Clear();
        PreTxtBox.Text = text;
        PreSubTitle.Text = WordsCountPrefix + StringUtils.CountWords(PreTxtBox.Text);
    }

    public void SetText(string[] files)
    {
        var text = string.Join('\n', files);
        SetText(text);
    }

    public void SetImage()
    {
        PreTxtBoxSv.Visibility = Visibility.Collapsed;
        PreSubTitle.Visibility = Visibility.Collapsed;
        if (ClipboardData.Data is System.Drawing.Image img)
        {
            PreImage.Visibility = Visibility.Visible;
            var im = img.ToBitmapImage();
            PreImage.Source = im;
        }
    }

    #endregion
}
