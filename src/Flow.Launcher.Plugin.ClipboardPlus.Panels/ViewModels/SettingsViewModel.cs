using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class SettingsViewModel : BaseModel
{
    private static string ClassName => nameof(SettingsViewModel);

    private readonly IClipboardPlus ClipboardPlus;

    private PluginInitContext? Context => ClipboardPlus.Context;

    private ISettings Settings => ClipboardPlus.Settings;

    public SettingsViewModel(IClipboardPlus clipboardPlus)
    {
        ClipboardPlus = clipboardPlus;
        _cacheFormatPreview = StringUtils.FormatImageName(Settings.CacheFormat, DateTime.Now);
        if (string.IsNullOrWhiteSpace(ClearKeyword))
        {
            ShowClearKeywordEmptyError();
        }
    }

    #region Commands

    #region Open Windows Clipboard Settings

    public ICommand OpenWindowsClipboardSettingsCommand => new RelayCommand(OpenWindowsClipboardSettings);

    private void OpenWindowsClipboardSettings(object? parameter)
    {
        Context.OpenAppUri("ms-settings:clipboard");
    }

    #endregion

    #region Open Cache Image Folder

    public ICommand OpenCacheImageFolderCommand => new RelayCommand(OpenCacheImageFolder);

    private void OpenCacheImageFolder(object? parameter)
    {
        if (!Directory.Exists(PathHelper.ImageCachePath))
        {
            Directory.CreateDirectory(PathHelper.ImageCachePath);
        }
        Context.OpenDirectory(PathHelper.ImageCachePath);
    }

    #endregion

    #region Clear Cache Image Folder

    public ICommand ClearCacheImageFolderCommand => new RelayCommand(ClearCacheImageFolder);

    private void ClearCacheImageFolder(object? parameter)
    {
        FileUtils.ClearImageCache(PathHelper.ImageCachePath);
    }

    #endregion

    #region Format String Insert

    public ICommand FormatStringInsertCommand => new RelayCommand(FormatStringInsert);

    private void FormatStringInsert(object? parameter)
    {
        if (parameter is object[] parameters && parameters.Length == 2)
        {
            if (parameters[0] is TextBox textBox && parameters[1] is string customString)
            {
                CacheFormat = CacheFormat.Insert(textBox.CaretIndex, customString ?? string.Empty);
            }
        }
    }

    #endregion

    #region Import & Export Records

    public ICommand ImportJsonRecordsCommand => new RelayCommand(ImportJsonRecords);

    private async void ImportJsonRecords(object? parameter)
    {
        ImportEnabled = false;

        var path = FileUtils.GetOpenJsonFile(ClipboardPlus);
        if (!string.IsNullOrEmpty(path))
        {
            await DatabaseHelper.ImportDatabase(ClipboardPlus, path);
        }

        ImportEnabled = true;
    }

    public ICommand ExportJsonRecordsCommand => new RelayCommand(ExportJsonRecords);

    private async void ExportJsonRecords(object? parameter)
    {
        ExportEnabled = false;

        var path = FileUtils.GetSaveJsonFile(ClipboardPlus);
        if (!string.IsNullOrEmpty(path))
        {
            await DatabaseHelper.ExportDatabase(ClipboardPlus, path);
        }

        ExportEnabled = true;
    }

    #endregion

    #region Restore to Default

    public ICommand RestoreToDefaultCommand => new RelayCommand(RestoreToDefault);

    private void RestoreToDefault(object? parameter)
    {
        var oldSyncWindowsClipboardHistory = SyncWindowsClipboardHistory;
        var oldUseWindowsClipboardHistoryOnly = UseWindowsClipboardHistoryOnly;

        Settings.RestoreToDefault();

        RestoreRecordOrders();
        RestoreClickActions();
        RestoreDefaultRichTextCopyOptions();
        RestoreDefaultImageCopyOptions();
        RestoreDefaultFilesCopyOptions();
        RestoreKeepTimes();

        base.OnPropertyChanged(nameof(ClearKeyword));
        base.OnPropertyChanged(nameof(MaxRecords));
        base.OnPropertyChanged(nameof(ActionTop));
        base.OnPropertyChanged(nameof(ShowNotification));

        base.OnPropertyChanged(nameof(SyncWindowsClipboardHistory));
        base.OnPropertyChanged(nameof(UseWindowsClipboardHistoryOnly));
        base.OnPropertyChanged(nameof(SyncWindowsClipboardHistoryEnabled));
        base.OnPropertyChanged(nameof(DatabasePanelVisibility));

        base.OnPropertyChanged(nameof(CacheImages));
        base.OnPropertyChanged(nameof(CacheFormat));
        base.OnPropertyChanged(nameof(EncryptData));
        base.OnPropertyChanged(nameof(KeepText));
        base.OnPropertyChanged(nameof(KeepImages));
        base.OnPropertyChanged(nameof(KeepFiles));

        ClipboardPlus.SaveSettingJsonStorage();

        var showRestartWarning = false;
        if (oldSyncWindowsClipboardHistory)
        {
            if (!ClipboardPlus.UseWindowsClipboardHistoryOnly)
            {
                ClipboardPlus.DisableWindowsClipboardHelper(true);
            }
            else
            {
                showRestartWarning = true;
            }
        }
        if (oldUseWindowsClipboardHistoryOnly && ClipboardPlus.UseWindowsClipboardHistoryOnly)
        {
            // If change to non-original value, show restart app warning
            showRestartWarning = true;
        }
        if (showRestartWarning && ShowRestartAppWarning())
        {
            Context.RestartApp();
        }
    }

    #endregion

    #endregion

    #region Dependency Properties

    #region Clear Keyword

    public string ClearKeyword
    {
        get => Settings.ClearKeyword;
        set
        {
            if (Settings.ClearKeyword == value)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowClearKeywordEmptyError();
                return;
            }
            Settings.ClearKeyword = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Max Records

    private const int MaximumMaxRecords = 100000;

#pragma warning disable CA1822 // Mark members as static
    public int MaxRecordsMaximum => MaximumMaxRecords;
#pragma warning restore CA1822 // Mark members as static

    public int MaxRecords
    {
        get => Settings.MaxRecords;
        set
        {
            if (Settings.MaxRecords == value)
            {
                return;
            }
            Settings.MaxRecords = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Record Order

    public List<RecordOrderLocalized> AllRecordOrders { get; } = RecordOrderLocalized.GetValues();

    public RecordOrder SelectedRecordOrder
    {
        get => Settings.RecordOrder;
        set
        {
            if (Settings.RecordOrder != value)
            {
                Settings.RecordOrder = value;
                OnPropertyChanged();
            }
        }
    }

    private void RestoreRecordOrders()
    {
        base.OnPropertyChanged(nameof(SelectedRecordOrder));
    }

    #endregion

    #region Action Top

    public bool ActionTop
    {
        get => Settings.ActionTop;
        set
        {
            if (Settings.ActionTop == value)
            {
                return;
            }
            Settings.ActionTop = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Show Notification

    public bool ShowNotification
    {
        get => Settings.ShowNotification;
        set
        {
            if (Settings.ShowNotification == value)
            {
                return;
            }
            Settings.ShowNotification = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Sync Windows Clipboard History

#pragma warning disable CA1822 // Mark members as static
    public Visibility SyncWindowsClipboardHistoryVisibility => WindowsClipboardHelper.IsClipboardHistorySupported()
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility DatabasePanelVisibility => UseWindowsClipboardHistoryOnly
        ? Visibility.Hidden
        : Visibility.Visible;
#pragma warning restore CA1822 // Mark members as static

    #region Sync Windows Clipboard History

    public bool SyncWindowsClipboardHistoryEnabled => !Settings.UseWindowsClipboardHistoryOnly;

    public bool SyncWindowsClipboardHistory
    {
        get => Settings.SyncWindowsClipboardHistory;
        set
        {
            if (Settings.SyncWindowsClipboardHistory == value)
            {
                return;
            }
            Settings.SyncWindowsClipboardHistory = value;
            OnPropertyChanged();
            if (!ClipboardPlus.UseWindowsClipboardHistoryOnly)
            {
                if (value)
                {
                    ClipboardPlus.EnableWindowsClipboardHelper(true);
                }
                else
                {
                    ClipboardPlus.DisableWindowsClipboardHelper(true);
                }
            }
            else if (ShowRestartAppWarning())
            {
                Context.RestartApp();
            }
        }
    }

    #endregion

    #region Use Windows Clipboard History Only

    public bool UseWindowsClipboardHistoryOnly
    {
        get => Settings.UseWindowsClipboardHistoryOnly;
        set
        {
            if (Settings.UseWindowsClipboardHistoryOnly == value)
            {
                return;
            }
            // If change to true, show warning message
            if (value && (!ShowUseWindowsClipboardHistoryOnlyWarning()))
            {
                return;
            }
            Settings.UseWindowsClipboardHistoryOnly = value;
            OnPropertyChanged();
            base.OnPropertyChanged(nameof(SyncWindowsClipboardHistoryEnabled));
            base.OnPropertyChanged(nameof(DatabasePanelVisibility));
            // If change to non-original value, show restart app warning
            if (value != ClipboardPlus.UseWindowsClipboardHistoryOnly && ShowRestartAppWarning())
            {
                Context.RestartApp();
            }
        }
    }

    #endregion

    #endregion

    #region Click Actions

    public List<ClickActionLocalized> AllClickActions { get; } = ClickActionLocalized.GetValues();

    public ClickAction SelectedClickAction
    {
        get => Settings.ClickAction;
        set
        {
            if (Settings.ClickAction != value)
            {
                Settings.ClickAction = value;
                OnPropertyChanged();
            }
        }
    }

    private void RestoreClickActions()
    {
        base.OnPropertyChanged(nameof(SelectedClickAction));
    }

    #endregion

    #region Default Rich Text Copy Option

    public List<DefaultRichTextCopyOptionLocalized> AllDefaultRichTextCopyOptions { get; } = DefaultRichTextCopyOptionLocalized.GetValues();

    public DefaultRichTextCopyOption SelectedDefaultRichTextCopyOption
    {
        get => Settings.DefaultRichTextCopyOption;
        set
        {
            if (Settings.DefaultRichTextCopyOption != value)
            {
                Settings.DefaultRichTextCopyOption = value;
                OnPropertyChanged();
            }
        }
    }

    private void RestoreDefaultRichTextCopyOptions()
    {
        base.OnPropertyChanged(nameof(SelectedDefaultRichTextCopyOption));
    }

    #endregion

    #region Default Image Copy Option

    public List<DefaultImageCopyOptionLocalized> AllDefaultImageCopyOptions { get; } = DefaultImageCopyOptionLocalized.GetValues();

    public DefaultImageCopyOption SelectedDefaultImageCopyOption
    {
        get => Settings.DefaultImageCopyOption;
        set
        {
            if (Settings.DefaultImageCopyOption != value)
            {
                Settings.DefaultImageCopyOption = value;
                OnPropertyChanged();
            }
        }
    }

    private void RestoreDefaultImageCopyOptions()
    {
        base.OnPropertyChanged(nameof(SelectedDefaultImageCopyOption));
    }

    #endregion

    #region Default Files Copy Option

    public List<DefaultFilesCopyOptionLocalized> AllDefaultFilesCopyOptions { get; } = DefaultFilesCopyOptionLocalized.GetValues();

    public DefaultFilesCopyOption SelectedDefaultFilesCopyOption
    {
        get => Settings.DefaultFilesCopyOption;
        set
        {
            if (Settings.DefaultFilesCopyOption != value)
            {
                Settings.DefaultFilesCopyOption = value;
                OnPropertyChanged();
            }
        }
    }

    private void RestoreDefaultFilesCopyOptions()
    {
        base.OnPropertyChanged(nameof(SelectedDefaultFilesCopyOption));
    }

    #endregion

    #region Cache Images

    public bool CacheImages
    {
        get => Settings.CacheImages;
        set
        {
            if (Settings.CacheImages == value)
            {
                return;
            }
            Settings.CacheImages = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Cache Format

    public string CacheFormat
    {
        get => Settings.CacheFormat;
        set
        {
            if (Settings.CacheFormat == value)
            {
                return;
            }
            Settings.CacheFormat = value;
            CacheFormatPreview = StringUtils.FormatImageName(value, DateTime.Now);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Cache Format Preview

    private string _cacheFormatPreview;
    public string CacheFormatPreview
    {
        get => _cacheFormatPreview;
        set
        {
            if (_cacheFormatPreview == value)
            {
                return;
            }
            _cacheFormatPreview = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Import & Export Records

    private bool _importEnabled = true;
    public bool ImportEnabled
    {
        get => _importEnabled;
        set
        {
            if (_importEnabled == value)
            {
                return;
            }
            _importEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _exportEnabled = true;
    public bool ExportEnabled
    {
        get => _exportEnabled;
        set
        {
            if (_exportEnabled == value)
            {
                return;
            }
            _exportEnabled = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Encrypt Data

    public bool EncryptData
    {
        get => Settings.EncryptData;
        set
        {
            if (Settings.EncryptData == value)
            {
                return;
            }
            Settings.EncryptData = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Keep Time

    #region Text

    public bool KeepText
    {
        get => Settings.KeepText;
        set
        {
            if (Settings.KeepText == value)
            {
                return;
            }
            Settings.KeepText = value;
            OnPropertyChanged();
        }
    }

    public List<KeepTimeLocalized> AllTextKeepTimes { get; } = KeepTimeLocalized.GetValues();

    public KeepTime SelectedTextKeepTime
    {
        get => Settings.TextKeepTime;
        set
        {
            if (Settings.TextKeepTime != value)
            {
                Settings.TextKeepTime = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Images

    public bool KeepImages
    {
        get => Settings.KeepImages;
        set
        {
            if (Settings.KeepImages == value)
            {
                return;
            }
            Settings.KeepImages = value;
            OnPropertyChanged();
        }
    }

    public List<KeepTimeLocalized> AllImagesKeepTimes { get; } = KeepTimeLocalized.GetValues();

    public KeepTime SelectedImagesKeepTime
    {
        get => Settings.ImagesKeepTime;
        set
        {
            if (Settings.ImagesKeepTime != value)
            {
                Settings.ImagesKeepTime = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Files

    public bool KeepFiles
    {
        get => Settings.KeepFiles;
        set
        {
            if (Settings.KeepFiles == value)
            {
                return;
            }
            Settings.KeepFiles = value;
            OnPropertyChanged();
        }
    }

    public List<KeepTimeLocalized> AllFilesKeepTimes { get; } = KeepTimeLocalized.GetValues();

    public KeepTime SelectedFilesKeepTime
    {
        get => Settings.FilesKeepTime;
        set
        {
            if (Settings.FilesKeepTime != value)
            {
                Settings.FilesKeepTime = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    private void RestoreKeepTimes()
    {
        base.OnPropertyChanged(nameof(SelectedTextKeepTime));
        base.OnPropertyChanged(nameof(SelectedImagesKeepTime));
        base.OnPropertyChanged(nameof(SelectedFilesKeepTime));
    }

    #endregion

    #endregion

    #region OnPropertyChanged

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);
        ClipboardPlus.SaveSettingJsonStorage();
        Context.LogDebug(ClassName, $"{propertyName} changed and save settings");
    }

    #endregion

    #region Message Box

    private void ShowClearKeywordEmptyError()
    {
        Context.ShowMsgBox(Localize.flowlauncher_plugin_clipboardplus_clear_keyword_empty_text(),
            Localize.flowlauncher_plugin_clipboardplus_clear_keyword_empty_caption(),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private bool ShowUseWindowsClipboardHistoryOnlyWarning()
    {
        return Context.ShowMsgBox(
            Localize.flowlauncher_plugin_clipboardplus_use_windows_clipboard_history_only_text(),
            Localize.flowlauncher_plugin_clipboardplus_use_windows_clipboard_history_only_caption(),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private bool ShowRestartAppWarning()
    {
        return Context.ShowMsgBox(Localize.flowlauncher_plugin_clipboardplus_restart_text(),
            Localize.flowlauncher_plugin_clipboardplus_restart_caption(),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    #endregion
}
