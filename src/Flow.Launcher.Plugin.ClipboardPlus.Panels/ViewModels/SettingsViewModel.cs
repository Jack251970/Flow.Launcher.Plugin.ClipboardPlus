using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class SettingsViewModel : BaseModel
{
    public IClipboardPlus ClipboardPlus { get; private set; }

    private PluginInitContext? Context => ClipboardPlus.Context;

    private ISettings Settings => ClipboardPlus.Settings;

    public SettingsViewModel(IClipboardPlus clipboardPlus)
    {
        ClipboardPlus = clipboardPlus;
        InitializeRecordOrderSelection();
        InitializeClickActionSelection();
        InitializeDefaultRichTextCopyOptionSelection();
        InitializeDefaultImageCopyOptionSelection();
        InitializeDefaultFilesCopyOptionSelection();
        InitializeCacheFormatPreview();
        InitializeKeepTimeSelection();
        ClipboardPlus.CultureInfoChanged += ClipboardPlus_CultureInfoChanged;
        if (string.IsNullOrEmpty(ClearKeyword))
        {
            ShowClearKeywordEmptyError();
        }
    }

    // TODO: Use new api for this.
    private void ShowClearKeywordEmptyError()
    {
        MessageBox.Show(Context?.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_keyword_empty_text") ?? "Clear keyword should not be empty!",
            Context?.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_keyword_empty_caption") ?? "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    #region Commands

    #region Open Windows Clipboard Settings

    public ICommand OpenWindowsClipboardSettingsCommand => new RelayCommand(OpenWindowsClipboardSettings);

    private void OpenWindowsClipboardSettings(object? parameter)
    {
        Context?.API.OpenAppUri("ms-settings:clipboard");
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
        Context?.API.OpenDirectory(PathHelper.ImageCachePath);
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
            if (value == string.Empty)
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

    private IReadOnlyList<EnumBindingModel<RecordOrder>> _recordOrders;
    public IReadOnlyList<EnumBindingModel<RecordOrder>> RecordOrders
    {
        get => _recordOrders;
        set
        {
            if (_recordOrders == value)
            {
                return;
            }
            _recordOrders = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<RecordOrder> _selectedRecordOrder;
    public EnumBindingModel<RecordOrder> SelectedRecordOrder
    {
        get => _selectedRecordOrder;
        set
        {
            if (Settings.RecordOrder == value.Value)
            {
                return;
            }
            _selectedRecordOrder = value;
            Settings.RecordOrder = value.Value;
            OnPropertyChanged();
        }
    }

    [MemberNotNull(nameof(_recordOrders),
        nameof(_selectedRecordOrder))]
    private void InitializeRecordOrderSelection()
    {
        _recordOrders = EnumBindingModel<RecordOrder>.CreateList(Context);

        _selectedRecordOrder = _recordOrders.First(x => x.Value == Settings.RecordOrder);
    }

    private void RefreshRecordOrders()
    {
        _recordOrders = EnumBindingModel<RecordOrder>.CreateList(Context);

        SelectedRecordOrder = RecordOrders.First(x => x.Value == Settings.RecordOrder);

        OnPropertyChanged(nameof(RecordOrders));
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

    #region Sync Windows Clipboard History

#pragma warning disable CA1822 // Mark members as static
    public Visibility SyncWindowsClipboardHistoryVisibility => WindowsClipboardHelper.IsClipboardHistorySupported()
        ? Visibility.Visible
        : Visibility.Collapsed;
#pragma warning restore CA1822 // Mark members as static

    #region Sync Windows Clipboard History

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
            if (value)
            {
                _ = ClipboardPlus.InitRecordsFromSystemAsync();
                ClipboardPlus.EnableWindowsClipboardHelper();
            }
            else
            {
                ClipboardPlus.DisableWindowsClipboardHelper();
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
            if (value && MessageBox.Show(
                Context?.API.GetTranslation("flowlauncher_plugin_clipboardplus_use_windows_clipboard_history_only_text") ??
                "If you enable this option, query records will fully match the Windows clipboard history. Records from the database will no longer be loaded, and records cannot be saved to the database.",
                Context?.API.GetTranslation("flowlauncher_plugin_clipboardplus_use_windows_clipboard_history_only_caption") ??
                "Are you sure you want to enable this option?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            Settings.UseWindowsClipboardHistoryOnly = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #endregion

    #region Click Actions

    private IReadOnlyList<EnumBindingModel<ClickAction>> _clickActions;
    public IReadOnlyList<EnumBindingModel<ClickAction>> ClickActions
    {
        get => _clickActions;
        set
        {
            if (_clickActions == value)
            {
                return;
            }
            _clickActions = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<ClickAction> _selectedClickAction;
    public EnumBindingModel<ClickAction> SelectedClickAction
    {
        get => _selectedClickAction;
        set
        {
            if (Settings.ClickAction == value.Value)
            {
                return;
            }
            _selectedClickAction = value;
            Settings.ClickAction = value.Value;
            OnPropertyChanged();
        }
    }

    [MemberNotNull(nameof(_clickActions),
        nameof(_selectedClickAction))]
    private void InitializeClickActionSelection()
    {
        _clickActions = EnumBindingModel<ClickAction>.CreateList(Context);

        _selectedClickAction = _clickActions.First(x => x.Value == Settings.ClickAction);
    }

    private void RefreshClickActions()
    {
        _clickActions = EnumBindingModel<ClickAction>.CreateList(Context);

        SelectedClickAction = ClickActions.First(x => x.Value == Settings.ClickAction);

        OnPropertyChanged(nameof(ClickActions));
    }

    #endregion

    #region Default Rich Text Copy Option

    private IReadOnlyList<EnumBindingModel<DefaultRichTextCopyOption>> _defaultRichTextCopyOptions;
    public IReadOnlyList<EnumBindingModel<DefaultRichTextCopyOption>> DefaultRichTextCopyOptions
    {
        get => _defaultRichTextCopyOptions;
        set
        {
            if (_defaultRichTextCopyOptions == value)
            {
                return;
            }
            _defaultRichTextCopyOptions = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<DefaultRichTextCopyOption> _selectedDefaultRichTextCopyOption;
    public EnumBindingModel<DefaultRichTextCopyOption> SelectedDefaultRichTextCopyOption
    {
        get => _selectedDefaultRichTextCopyOption;
        set
        {
            if (Settings.DefaultRichTextCopyOption == value.Value)
            {
                return;
            }
            _selectedDefaultRichTextCopyOption = value;
            Settings.DefaultRichTextCopyOption = value.Value;
            OnPropertyChanged();
        }
    }

    [MemberNotNull(nameof(_defaultRichTextCopyOptions),
        nameof(_selectedDefaultRichTextCopyOption))]
    private void InitializeDefaultRichTextCopyOptionSelection()
    {
        _defaultRichTextCopyOptions = EnumBindingModel<DefaultRichTextCopyOption>.CreateList(Context);

        _selectedDefaultRichTextCopyOption = _defaultRichTextCopyOptions.First(x => x.Value == Settings.DefaultRichTextCopyOption);
    }

    private void RefreshDefaultRichTextCopyOptions()
    {
        _defaultRichTextCopyOptions = EnumBindingModel<DefaultRichTextCopyOption>.CreateList(Context);

        SelectedDefaultRichTextCopyOption = DefaultRichTextCopyOptions.First(x => x.Value == Settings.DefaultRichTextCopyOption);

        OnPropertyChanged(nameof(DefaultRichTextCopyOptions));
    }

    #endregion

    #region Default Image Copy Option

    private IReadOnlyList<EnumBindingModel<DefaultImageCopyOption>> _defaultImageCopyOptions;
    public IReadOnlyList<EnumBindingModel<DefaultImageCopyOption>> DefaultImageCopyOptions
    {
        get => _defaultImageCopyOptions;
        set
        {
            if (_defaultImageCopyOptions == value)
            {
                return;
            }
            _defaultImageCopyOptions = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<DefaultImageCopyOption> _selectedDefaultImageCopyOption;
    public EnumBindingModel<DefaultImageCopyOption> SelectedDefaultImageCopyOption
    {
        get => _selectedDefaultImageCopyOption;
        set
        {
            if (Settings.DefaultImageCopyOption == value.Value)
            {
                return;
            }
            _selectedDefaultImageCopyOption = value;
            Settings.DefaultImageCopyOption = value.Value;
            OnPropertyChanged();
        }
    }

    [MemberNotNull(nameof(_defaultImageCopyOptions),
        nameof(_selectedDefaultImageCopyOption))]
    private void InitializeDefaultImageCopyOptionSelection()
    {
        _defaultImageCopyOptions = EnumBindingModel<DefaultImageCopyOption>.CreateList(Context);

        _selectedDefaultImageCopyOption = _defaultImageCopyOptions.First(x => x.Value == Settings.DefaultImageCopyOption);
    }

    private void RefreshDefaultImageCopyOptions()
    {
        _defaultImageCopyOptions = EnumBindingModel<DefaultImageCopyOption>.CreateList(Context);

        SelectedDefaultImageCopyOption = DefaultImageCopyOptions.First(x => x.Value == Settings.DefaultImageCopyOption);

        OnPropertyChanged(nameof(DefaultImageCopyOptions));
    }

    #endregion

    #region Default Files Copy Option

    private IReadOnlyList<EnumBindingModel<DefaultFilesCopyOption>> _defaultFilesCopyOptions;
    public IReadOnlyList<EnumBindingModel<DefaultFilesCopyOption>> DefaultFilesCopyOptions
    {
        get => _defaultFilesCopyOptions;
        set
        {
            if (_defaultFilesCopyOptions == value)
            {
                return;
            }
            _defaultFilesCopyOptions = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<DefaultFilesCopyOption> _selectedDefaultFilesCopyOption;
    public EnumBindingModel<DefaultFilesCopyOption> SelectedDefaultFilesCopyOption
    {
        get => _selectedDefaultFilesCopyOption;
        set
        {
            if (Settings.DefaultFilesCopyOption == value.Value)
            {
                return;
            }
            _selectedDefaultFilesCopyOption = value;
            Settings.DefaultFilesCopyOption = value.Value;
            OnPropertyChanged();
        }
    }

    [MemberNotNull(nameof(_defaultFilesCopyOptions),
        nameof(_selectedDefaultFilesCopyOption))]
    private void InitializeDefaultFilesCopyOptionSelection()
    {
        _defaultFilesCopyOptions = EnumBindingModel<DefaultFilesCopyOption>.CreateList(Context);

        _selectedDefaultFilesCopyOption = _defaultFilesCopyOptions.First(x => x.Value == Settings.DefaultFilesCopyOption);
    }

    private void RefreshDefaultFilesCopyOptions()
    {
        _defaultFilesCopyOptions = EnumBindingModel<DefaultFilesCopyOption>.CreateList(Context);

        SelectedDefaultFilesCopyOption = DefaultFilesCopyOptions.First(x => x.Value == Settings.DefaultFilesCopyOption);

        OnPropertyChanged(nameof(DefaultFilesCopyOptions));
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

    [MemberNotNull(nameof(_cacheFormatPreview))]
    public void InitializeCacheFormatPreview()
    {
        _cacheFormatPreview = StringUtils.FormatImageName(Settings.CacheFormat, DateTime.Now);
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

    #region Keep Time

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

    private IReadOnlyList<EnumBindingModel<KeepTime>> _textKeepTimes;
    public IReadOnlyList<EnumBindingModel<KeepTime>> TextKeepTimes
    {
        get => _textKeepTimes;
        set
        {
            if (_textKeepTimes == value)
            {
                return;
            }
            _textKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<KeepTime> _selectedTextKeepTime;
    public EnumBindingModel<KeepTime> SelectedTextKeepTime
    {
        get => _selectedTextKeepTime;
        set
        {
            if (Settings.TextKeepTime == value.Value)
            {
                return;
            }
            _selectedTextKeepTime = value;
            Settings.TextKeepTime = value.Value;
            OnPropertyChanged();
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

    private IReadOnlyList<EnumBindingModel<KeepTime>> _imagesKeepTimes;
    public IReadOnlyList<EnumBindingModel<KeepTime>> ImagesKeepTimes
    {
        get => _imagesKeepTimes;
        set
        {
            if (_imagesKeepTimes == value)
            {
                return;
            }
            _imagesKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<KeepTime> _selectedImagesKeepTime;
    public EnumBindingModel<KeepTime> SelectedImagesKeepTime
    {
        get => _selectedImagesKeepTime;
        set
        {
            if (Settings.ImagesKeepTime == value.Value)
            {
                return;
            }
            _selectedImagesKeepTime = value;
            Settings.ImagesKeepTime = value.Value;
            OnPropertyChanged();
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

    private IReadOnlyList<EnumBindingModel<KeepTime>> _filesKeepTimes;
    public IReadOnlyList<EnumBindingModel<KeepTime>> FilesKeepTimes
    {
        get => _filesKeepTimes;
        set
        {
            if (_filesKeepTimes == value)
            {
                return;
            }
            _filesKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<KeepTime> _selectedFilesKeepTime;
    public EnumBindingModel<KeepTime> SelectedFilesKeepTime
    {
        get => _selectedFilesKeepTime;
        set
        {
            if (Settings.FilesKeepTime == value.Value)
            {
                return;
            }
            _selectedFilesKeepTime = value;
            Settings.FilesKeepTime = value.Value;
            OnPropertyChanged();
        }
    }

    #endregion

    [MemberNotNull(nameof(_textKeepTimes),
        nameof(_imagesKeepTimes),
        nameof(_filesKeepTimes),
        nameof(_selectedTextKeepTime),
        nameof(_selectedImagesKeepTime),
        nameof(_selectedFilesKeepTime))]
    public void InitializeKeepTimeSelection()
    {
        _textKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);
        _imagesKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);
        _filesKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);

        _selectedTextKeepTime = _textKeepTimes.First(x => x.Value == Settings.TextKeepTime);
        _selectedImagesKeepTime = _imagesKeepTimes.First(x => x.Value == Settings.ImagesKeepTime);
        _selectedFilesKeepTime = _filesKeepTimes.First(x => x.Value == Settings.FilesKeepTime);
    }

    private void RefreshKeepTimes()
    {
        _textKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);
        _imagesKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);
        _filesKeepTimes = EnumBindingModel<KeepTime>.CreateList(Context);

        SelectedTextKeepTime = TextKeepTimes.First(x => x.Value == Settings.TextKeepTime);
        SelectedImagesKeepTime = ImagesKeepTimes.First(x => x.Value == Settings.ImagesKeepTime);
        SelectedFilesKeepTime = FilesKeepTimes.First(x => x.Value == Settings.FilesKeepTime);

        OnPropertyChanged(nameof(TextKeepTimes));
        OnPropertyChanged(nameof(ImagesKeepTimes));
        OnPropertyChanged(nameof(FilesKeepTimes));
    }

    #endregion

    #endregion

    #region OnPropertyChanged

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);
        ClipboardPlus.SaveSettingJsonStorage();
    }

    #endregion

    #region Culture Info

    private void ClipboardPlus_CultureInfoChanged(object? sender, CultureInfo cultureInfo)
    {
        RefreshRecordOrders();
        RefreshClickActions();
        RefreshDefaultRichTextCopyOptions();
        RefreshDefaultImageCopyOptions();
        RefreshDefaultFilesCopyOptions();
        RefreshKeepTimes();
    }

    #endregion
}
