﻿using System;
using System.Windows;
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

    private void RefreshStatus()
    {
        Words = StringUtils.CountWords(PreviewPlainText).ToString();
    }

    #endregion

    #region Dimension

    public Visibility DimensionVisibility => ClipboardData.DataType == DataType.Image
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Dimension => ClipboardData.Data is BitmapSource image ?
        $"{image.PixelWidth}x{image.PixelHeight}" :
        ClipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_unknown");

    #endregion

    #region Count

    public Visibility CountVisibility => ClipboardData.DataType == DataType.Files
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string Count => ClipboardData.Data is string[] files ?
        files.Length.ToString() :
        ClipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_unknown");

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
        Context.LogDebug(ClassName, $"Preview {ClipboardData.DataType} content: {ClipboardData.HashId}");
    }

    #endregion
}
