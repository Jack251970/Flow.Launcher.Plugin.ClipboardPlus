﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class SettingsViewModel : BaseModel, IDisposable
{
    public Settings Settings { get; set; }

    private PluginInitContext? Context { get; set; }

    public SettingsViewModel(PluginInitContext? context, Settings settings)
    {
        Context = context;
        Settings = settings;

        InitializeRecordOrderSelection();
        InitializeClickActionSelection();
        InitializeCacheFormatPreview();
        InitializeKeepTimeSelection();
    }

    public void OnCultureInfoChanged(CultureInfo _)
    {
        RefreshRecordOrders();
        RefreshClickActions();
        RefreshKeepTimes();
    }

    #region Commands

    #region Open Cache Image Folder

    public ICommand OpenCacheImageFolderCommand => new RelayCommand(OpenCacheImageFolder);

    private void OpenCacheImageFolder(object? parameter)
    {
        Context?.API.OpenDirectory(PathHelper.ImageCachePath);
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

    #endregion

    #region Properties

    #region Clear Keyword

    public string ClearKeyword
    {
        get => Settings.ClearKeyword;
        set
        {
            Settings.ClearKeyword = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Max Records

    public int MaxRecords
    {
        get => Settings.MaxRecords;
        set
        {
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

    #region Click Actions

    private IReadOnlyList<EnumBindingModel<ClickAction>> _clickActions;
    public IReadOnlyList<EnumBindingModel<ClickAction>> ClickActions
    {
        get => _clickActions;
        set
        {
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

    #region Cache Images

    public bool CacheImages
    {
        get => Settings.CacheImages;
        set
        {
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

    #region Keep Time

    #region Text

    public bool KeepText
    {
        get => Settings.KeepText;
        set
        {
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

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);
        Context?.API.SaveSettingJsonStorage<Settings>();
    }

    #endregion

    #region IDisposable Interface

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Settings = null!;
            Context = null!;
            RecordOrders = null!;
            ClickActions = null!;
            TextKeepTimes = null!;
            ImagesKeepTimes = null!;
            FilesKeepTimes = null!;
        }
    }

    #endregion
}