using ClipboardPlus.Core;
using Flow.Launcher.Plugin;
using System.IO;
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

namespace ClipboardPlus.Panels3;

public sealed partial class PreviewPanel : UserControl
{
    private ClipboardData _clipboardData;
    private PluginInitContext? _context;
    private DirectoryInfo CacheDir { get; set; }
    private Action<ClipboardData>? DeleteOneRecord { get; set; }
    private Action<ClipboardData>? CopyRecord { get; set; }
    private Action<ClipboardData>? PinRecord { get; set; }
    private int OldScore { get; set; }
    private bool Ready { get; set; } = false;
    private const string WordsCountPrefix = "Words Count: ";

    public PreviewPanel(
        ClipboardData clipboardData,
        PluginInitContext context,
        DirectoryInfo cacheDir,
        Action<ClipboardData> delAction,
        Action<ClipboardData> copyAction,
        Action<ClipboardData> pinAction,
        string _ = CbColors.Blue500
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
        PreImageMenuFlyout = GetRightTappedMenu();
        Ready = true;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// </summary>
    public PreviewPanel()
    {
        _context = null;
        CacheDir = null!;
        InitializeComponent();
        PreImageMenuFlyout = GetRightTappedMenu();
        Ready = true;
    }

    private void SetBtnIcon()
    {
        SetPinButtonIcon();
    }

    public void SetContent()
    {
        TxtBoxPre.Visibility = Visibility.Visible;
        PreImage.Visibility = Visibility.Collapsed;
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
                TxtBoxPre.Visibility = Visibility.Collapsed;
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
        TxtBoxPre.Text = "";
        TxtBoxPre.Text = string.IsNullOrWhiteSpace(s) ? _clipboardData.Text : s;
        TextBlockWordCount.Text = WordsCountPrefix + Utils.CountWords(TxtBoxPre.Text);
    }

    public void SetImage()
    {
        if (_clipboardData.Data is not System.Drawing.Image img)
        {
            return;
        }

        // TODO: Display image.
        //PreImage.Source = img.ToBitmapImage();
    }

    #region Image Viewer

    private readonly MenuFlyout PreImageMenuFlyout;

    private void PreImage_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            PreImageMenuFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private MenuFlyout GetRightTappedMenu()
    {
        var menuFlyout = new MenuFlyout();
        var imageCacheItem = new MenuFlyoutItem
        {
            Text = "Save in cache folder",
            Icon = new FontIcon() { Glyph = "\uE74E" }
        };
        imageCacheItem.Click += ImSaveAs_Click;
        menuFlyout.Items.Add(imageCacheItem);

        return menuFlyout;
    }

    private void ImSaveAs_Click(object sender, RoutedEventArgs e)
    {
        Utils.SaveImageCache(_clipboardData, CacheDir);
    }

    #endregion

    #region Status Bar

    private void TxtBoxPre_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.SelectAll();
    }

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            TextBlockWordCount.Text = WordsCountPrefix + TxtBoxPre.Text.Length;
        }
    }

    #endregion

    #region Copy Button

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        // if textbox is visible, it means the record is a text ot files, change the data to text
        if (TxtBoxPre.Visibility == Visibility.Visible)
        {
            _clipboardData.Data = TxtBoxPre.Text;
        }

        CopyRecord?.Invoke(_clipboardData);
    }

    #endregion

    #region Pin & Unpin Button

    private readonly FontIcon PinFontIcon = new() { Glyph = "\uE718" };
    private readonly FontIcon UnpinFontIcon = new() { Glyph = "\uE77A" };

    private void SetPinButtonIcon()
    {
        BtnPin.Content = _clipboardData.Pined ? UnpinFontIcon : PinFontIcon;
    }

    private void BtnPin_Click(object sender, RoutedEventArgs e)
    {
        _clipboardData.Pined = !_clipboardData.Pined;
        _clipboardData.Score = _clipboardData.Pined ? int.MaxValue : _clipboardData.InitScore;
        PinRecord?.Invoke(_clipboardData);
    }

    #endregion

    #region Delete Button

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        DeleteOneRecord?.Invoke(_clipboardData);
    }

    #endregion
}
