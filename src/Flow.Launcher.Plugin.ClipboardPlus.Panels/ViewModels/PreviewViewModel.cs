using System.Windows;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class PreviewViewModel : BaseModel
{
    public IClipboardPlus ClipboardPlus { get; private set; }

    private PluginInitContext? Context => ClipboardPlus.Context;

    private ClipboardData ClipboardData;

    public PreviewViewModel(IClipboardPlus clipboardPlus, ClipboardData clipboardData)
    {
        ClipboardPlus = clipboardPlus;
        ClipboardData = clipboardData;
        InitializeContent();
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

    public Visibility UnicodeTextPreviewVisibility => ClipboardData.DataType == DataType.UnicodeText || ClipboardData.DataType == DataType.Files
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewUnicodeText = string.Empty;
    public string PreviewUnicodeText
    {
        get => _previewUnicodeText;
        set
        {
            _previewUnicodeText = value;
            RefreshStatus();
            OnPropertyChanged();
        }
    }

    public Visibility RichTextPreviewVisibility => ClipboardData.DataType == DataType.RichText
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewRichText = string.Empty;
    public string PreviewRichText
    {
        get => _previewRichText;
        set
        {
            _previewRichText = value;
            RefreshStatus();
            OnPropertyChanged();
        }
    }

    #endregion

    #region Status Preview

    public Visibility StatusPreviewVisibility => ClipboardData.DataType == DataType.UnicodeText || ClipboardData.DataType == DataType.RichText
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewStatus = string.Empty;
    public string PreviewStatus
    {
        get => _previewStatus;
        set
        {
            _previewStatus = value;
            OnPropertyChanged();
        }
    }

    private void RefreshStatus()
    {
        PreviewStatus = Context.GetTranslation("flowlauncher_plugin_clipboardplus_words_count_prefix") +
            StringUtils.CountWords(PreviewUnicodeText);
    }

    #endregion

    private void InitializeContent()
    {
        switch (ClipboardData.DataType)
        {
            case DataType.UnicodeText:
            case DataType.Files:
                PreviewUnicodeText = ClipboardData.DataToString(false) ?? string.Empty;
                break;
            case DataType.RichText:
                _previewUnicodeText = ClipboardData.UnicodeTextToString(false) ?? string.Empty;
                PreviewRichText = ClipboardData.DataToString(false) ?? string.Empty;
                break;
            case DataType.Image:
                PreviewImage = ClipboardData.DataToImage();
                break;
            default:
                break;
        }
    }

    #endregion
}
