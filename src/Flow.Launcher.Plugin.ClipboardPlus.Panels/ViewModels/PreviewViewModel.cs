using System.Globalization;
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
        ClipboardPlus.CultureInfoChanged += ClipboardPlus_CultureInfoChanged;
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

    public Visibility TextPreviewVisibility => ClipboardData.DataType == DataType.Text || ClipboardData.DataType == DataType.Files
        ? Visibility.Visible
        : Visibility.Collapsed;

    private string _previewText = string.Empty;
    public string PreviewText
    {
        get => _previewText;
        set
        {
            _previewText = value;
            RefreshStatus();
            OnPropertyChanged();
        }
    }

    #endregion

    #region Status Preview

    public Visibility StatusPreviewVisibility => ClipboardData.DataType == DataType.Text
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
            StringUtils.CountWords(PreviewText);
    }

    #endregion

    private void InitializeContent()
    {
        switch (ClipboardData.DataType)
        {
            case DataType.Text:
            case DataType.Files:
                PreviewText = ClipboardData.DataToString() ?? string.Empty;
                PreviewImage = null;
                break;
            case DataType.Image:
                PreviewText = string.Empty;
                PreviewImage = ClipboardData.DataToImage();
                break;
            default:
                break;
        }
    }

    #endregion

    #region Culture Info

    private void ClipboardPlus_CultureInfoChanged(object? sender, CultureInfo cultureInfo)
    {
        RefreshStatus();
    }

    #endregion
}
