using FluentIcons.WPF;
using FluentIcons.Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels;

public partial class PreviewPanel : UserControl
{
    private ClipboardData ClipboardData;
    private readonly PluginInitContext Context;
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
        Action<ClipboardData> delAction,
        Action<ClipboardData> copyAction,
        Action<ClipboardData> pinAction
    )
    {
        ClipboardData = clipboardData;
        Context = context;
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
        Context = null!;
        InitializeComponent();
        Ready = true;
    }

    #region Setters

    private void SetBtnIcon()
    {
        BtnPin.Content = ClipboardData.Pinned ? UnpinFluentIcon : PinFluentIcon;
    }

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
        TextBlockWordCount.Text = WordsCountPrefix + StringUtils.CountWords(TxtBoxPre.Text);
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

    #region Dockpanel

    private bool needRefreshWidth = true;
    private double totalRequiredWidth = 0;

    private void DockPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!Ready)
        {
            return;
        }

        // If the DockPanel's width is less than the total required width, hide the TextBlock so that we can use the buttons
        if (needRefreshWidth)
        {
            if (TextBlockWordCount.Visibility == Visibility.Visible)
            {
                totalRequiredWidth = TextBlockWordCount.ActualWidth + TextBlockWordCount.Margin.Left + TextBlockWordCount.Margin.Right +
                    ButtonsStackPanel.ActualWidth + ButtonsStackPanel.Margin.Left + ButtonsStackPanel.Margin.Right;
                needRefreshWidth = false;
            }
        }

        TextBlockWordCount.Visibility = e.NewSize.Width < totalRequiredWidth ? Visibility.Collapsed : Visibility.Visible;
    }

    #endregion

    #region Word Count Textblock

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            TextBlockWordCount.Text = WordsCountPrefix + TxtBoxPre.Text.Length;
            needRefreshWidth = true;
        }
    }

    #endregion

    #region Buttons

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        // if textbox is visible, it means the record is a text ot files, change the data to text
        if (TxtBoxPre.IsVisible)
        {
            ClipboardData.Data = TxtBoxPre.Text;
        }
        CopyRecord?.Invoke(ClipboardData);
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        ClipboardData.Pinned = !ClipboardData.Pinned;
        ClipboardData.Score = ClipboardData.Pinned ? int.MaxValue : ClipboardData.InitScore;
        PinRecord?.Invoke(ClipboardData);
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        DeleteOneRecord?.Invoke(ClipboardData);
    }

    #endregion
}
