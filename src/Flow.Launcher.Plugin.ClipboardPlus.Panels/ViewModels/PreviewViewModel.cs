using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class PreviewViewModel : BaseModel
{
    private static string ClassName => nameof(PreviewViewModel);

    private readonly IClipboardPlus ClipboardPlus;

    private PluginInitContext? Context => ClipboardPlus.Context;

    private ClipboardData ClipboardData;

    public PreviewViewModel(IClipboardPlus clipboardPlus, ClipboardData clipboardData)
    {
        ClipboardPlus = clipboardPlus;
        ClipboardData = clipboardData;
        _isTextPreviewEnabled = ClipboardData.DataType == DataType.Files || ClipboardData.DataType == DataType.Image;
    }

    public void InitializeContent()
    {
        switch (ClipboardData.DataType)
        {
            case DataType.PlainText:
                _previewPlainText = ClipboardData.DataToString(false) ?? string.Empty;
                RefreshWordsCount();
                if (_wordsCount!.Value < MaxTextPreviewWords)
                {
                    IsTextPreviewEnabled = true;
                }
                break;
            case DataType.Files:
                PreviewPlainText = ClipboardData.DataToString(false) ?? string.Empty;
                break;
            case DataType.RichText:
                _previewPlainText = ClipboardData.PlainTextToString(false) ?? string.Empty;
                _previewRichText = ClipboardData.DataToString(false) ?? string.Empty;
                RefreshWordsCount();
                if (_wordsCount!.Value < MaxTextPreviewWords)
                {
                    IsTextPreviewEnabled = true;
                }
                break;
            case DataType.Image:
                PreviewImage = ClipboardData.DataToImage();
                break;
            default:
                break;
        }
        Context.LogDebug(ClassName, $"Preview {ClipboardData.DataType} content: {ClipboardData.HashId}");
    }

    #region Dependency Properties

    #region Image Preview

    public Visibility ImagePreviewVisibility => ClipboardData.DataType == DataType.Image
        ? Visibility.Visible
        : Visibility.Collapsed;

    private ImageSource? _previewImage;
    public ImageSource? PreviewImage
    {
        get => _previewImage;
        set
        {
            _previewImage = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Text Preview

    public Visibility PlainTextPreviewVisibility => IsTextPreviewEnabled && (ClipboardData.DataType == DataType.PlainText || ClipboardData.DataType == DataType.Files)
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewPlainText = string.Empty;
    public string PreviewPlainText
    {
        get => _previewPlainText;
        set
        {
            _previewPlainText = value;
            OnPropertyChanged();
        }
    }

    public Visibility RichTextPreviewVisibility => IsTextPreviewEnabled && ClipboardData.DataType == DataType.RichText
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewRichText = string.Empty;
    public string PreviewRichText
    {
        get => _previewRichText;
        set
        {
            _previewRichText = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Show Text Preview

    private const int MaxTextPreviewWords = 10000;

    private bool _isTextPreviewEnabled = false;
    public bool IsTextPreviewEnabled
    {
        get => _isTextPreviewEnabled;
        set
        {
            _isTextPreviewEnabled = value;
            OnPropertyChanged(nameof(ShowTextPreviewButtonVisibility));
            if (ClipboardData.DataType == DataType.RichText)
            {
                OnPropertyChanged(nameof(RichTextPreviewVisibility));
                OnPropertyChanged(nameof(PreviewRichText));
            }
            else
            {
                OnPropertyChanged(nameof(PlainTextPreviewVisibility));
                OnPropertyChanged(nameof(PreviewPlainText));
            }
        }
    }

    public Visibility ShowTextPreviewButtonVisibility => !IsTextPreviewEnabled
        ? Visibility.Visible
        : Visibility.Collapsed;

    public ICommand ShowTextPreviewCommand => new RelayCommand(ShowTextPreview);

    private void ShowTextPreview(object? parameter)
    {
        IsTextPreviewEnabled = true;
    }

    #endregion

    #region Source

    public Visibility SourceVisibility => ClipboardData.SenderApp != null
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Source => ClipboardData.SenderApp ?? string.Empty;

    #endregion

    #region Date

    public Visibility DateVisibility => ClipboardData.CreateTime != DateTime.MinValue
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Date => ClipboardData.CreateTime.ToString(ClipboardPlus.CultureInfo);

    #endregion

    #region Type

    public Visibility TypeVisibility => Visibility.Visible;

    public string Type => ResourceHelper.GetString(ClipboardPlus, ClipboardData.DataType);

    #endregion

    #region Words

    private int? _wordsCount = null;

    public Visibility WordsVisibility => ClipboardData.DataType == DataType.PlainText || ClipboardData.DataType == DataType.RichText
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _words = string.Empty;
    public string Words
    {
        get => _words;
        set
        {
            _words = value;
            OnPropertyChanged();
        }
    }

    private void RefreshWordsCount()
    {
        if (_wordsCount.HasValue)
            return;

        _wordsCount = StringUtils.CountWords(PreviewPlainText);
        Words = _wordsCount!.Value.ToString();
    }

    #endregion

    #region Dimension

    public Visibility DimensionVisibility => ClipboardData.DataType == DataType.Image
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Dimension => ClipboardData.Data is BitmapSource image ?
        $"{image.PixelWidth}x{image.PixelHeight}" :
        Localize.flowlauncher_plugin_clipboardplus_unknown();

    #endregion

    #region Count

    public Visibility CountVisibility => ClipboardData.DataType == DataType.Files
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Count => ClipboardData.Data is string[] files ?
        files.Length.ToString() :
        Localize.flowlauncher_plugin_clipboardplus_unknown();

    #endregion

    #endregion
}
