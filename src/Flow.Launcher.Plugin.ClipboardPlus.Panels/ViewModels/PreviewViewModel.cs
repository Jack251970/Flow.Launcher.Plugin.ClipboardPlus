using System.Windows;
using System.Windows.Media;

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

    public Visibility PlainTextPreviewVisibility => ClipboardData.DataType == DataType.PlainText || ClipboardData.DataType == DataType.Files
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewPlainText = string.Empty;
    public string PreviewPlainText
    {
        get => _previewPlainText;
        set
        {
            _previewPlainText = value;
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

    public Visibility StatusPreviewVisibility => ClipboardData.DataType == DataType.PlainText || ClipboardData.DataType == DataType.RichText
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
            StringUtils.CountWords(PreviewPlainText);
    }

    #endregion

    private void InitializeContent()
    {
        switch (ClipboardData.DataType)
        {
            case DataType.PlainText:
            case DataType.Files:
                PreviewPlainText = ClipboardData.DataToString(false) ?? string.Empty;
                break;
            case DataType.RichText:
                _previewPlainText = ClipboardData.PlainTextToString(false) ?? string.Empty;
                PreviewRichText = ClipboardData.DataToString(false) ?? string.Empty;
                break;
            case DataType.Image:
                PreviewImage = ClipboardData.DataToImage();
                break;
            default:
                break;
        }
        Context?.API.LogDebug(ClassName, $"Preview {ClipboardData.DataType} content: {ClipboardData.HashId}");
    }

    #endregion
}
