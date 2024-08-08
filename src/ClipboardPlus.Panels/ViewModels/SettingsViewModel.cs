using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace ClipboardPlus.Panels.ViewModels;

public class SettingsViewModel : BaseModel, IDisposable
{
    public Settings Settings { get; set; }

    private PluginInitContext? Context { get; set; }

    private Func<Task>? ReloadDataAsync { get; set; }
    private Action? SaveSettings { get; set; }

    public SettingsViewModel(PluginInitContext? context, Settings settings, Func<Task>? func, Action? action)
    {
        Context = context;
        Settings = settings;

        ReloadDataAsync = func;
        SaveSettings = action;

        InitializeRecordOrderSelection();
        InitializeClickActionSelection();
        InitializeCacheFormatPreview();
        InitializeKeepTimeSelection();
    }

    public void OnCultureInfoChanged(CultureInfo _)
    {
        ReloadRecordOrders();
        ReloadClickActions();
        ReloadKeepTimes();
    }

    protected void OnPropertyChanged(bool reload, [CallerMemberName] string propertyName = "")
    {
        OnPropertyChanged(propertyName);
        // TODO: Use Context.API instead.
        // Context.API.SaveSettingJsonStorage<Settings>();
        SaveSettings?.Invoke();
        if (reload)
        {
            ReloadDataAsync?.Invoke();
        }
    }

    #region Clear Keyword

    public string ClearKeyword
    {
        get => Settings.ClearKeyword;
        set
        {
            Settings.ClearKeyword = value;
            OnPropertyChanged(false);
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
            OnPropertyChanged(false);
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
            OnPropertyChanged(true);
        }
    }

    [MemberNotNull(nameof(_recordOrders),
        nameof(_selectedRecordOrder))]
    private void InitializeRecordOrderSelection()
    {
        _recordOrders = EnumBindingModel<RecordOrder>.CreateList(Context);

        _selectedRecordOrder = _recordOrders.First(x => x.Value == Settings.RecordOrder);
    }

    private void ReloadRecordOrders()
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
            OnPropertyChanged(false);
        }
    }

    [MemberNotNull(nameof(_clickActions),
        nameof(_selectedClickAction))]
    private void InitializeClickActionSelection()
    {
        _clickActions = EnumBindingModel<ClickAction>.CreateList(Context);

        _selectedClickAction = _clickActions.First(x => x.Value == Settings.ClickAction);
    }

    private void ReloadClickActions()
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
            OnPropertyChanged(false);
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
            OnPropertyChanged(false);
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
            OnPropertyChanged(true);
        }
    }

    private IReadOnlyList<EnumBindingModel<RecordKeepTime>> _textKeepTimes;
    public IReadOnlyList<EnumBindingModel<RecordKeepTime>> TextKeepTimes
    {
        get => _textKeepTimes;
        set
        {
            _textKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<RecordKeepTime> _selectedTextKeepTime;
    public EnumBindingModel<RecordKeepTime> SelectedTextKeepTime
    {
        get => _selectedTextKeepTime;
        set
        {
            _selectedTextKeepTime = value;
            Settings.TextKeepTime = value.Value;
            OnPropertyChanged(true);
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
            OnPropertyChanged(true);
        }
    }

    private IReadOnlyList<EnumBindingModel<RecordKeepTime>> _imagesKeepTimes;
    public IReadOnlyList<EnumBindingModel<RecordKeepTime>> ImagesKeepTimes
    {
        get => _imagesKeepTimes;
        set
        {
            _imagesKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<RecordKeepTime> _selectedImagesKeepTime;
    public EnumBindingModel<RecordKeepTime> SelectedImagesKeepTime
    {
        get => _selectedImagesKeepTime;
        set
        {
            _selectedImagesKeepTime = value;
            Settings.ImagesKeepTime = value.Value;
            OnPropertyChanged(true);
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
            OnPropertyChanged(true);
        }
    }

    private IReadOnlyList<EnumBindingModel<RecordKeepTime>> _filesKeepTimes;
    public IReadOnlyList<EnumBindingModel<RecordKeepTime>> FilesKeepTimes
    {
        get => _filesKeepTimes;
        set
        {
            _filesKeepTimes = value;
            OnPropertyChanged();
        }
    }

    private EnumBindingModel<RecordKeepTime> _selectedFilesKeepTime;
    public EnumBindingModel<RecordKeepTime> SelectedFilesKeepTime
    {
        get => _selectedFilesKeepTime;
        set
        {
            _selectedFilesKeepTime = value;
            Settings.FilesKeepTime = value.Value;
            OnPropertyChanged(true);
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
        _textKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);
        _imagesKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);
        _filesKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);

        _selectedTextKeepTime = _textKeepTimes.First(x => x.Value == Settings.TextKeepTime);
        _selectedImagesKeepTime = _imagesKeepTimes.First(x => x.Value == Settings.ImagesKeepTime);
        _selectedFilesKeepTime = _filesKeepTimes.First(x => x.Value == Settings.FilesKeepTime);
    }

    private void ReloadKeepTimes()
    {
        _textKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);
        _imagesKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);
        _filesKeepTimes = EnumBindingModel<RecordKeepTime>.CreateList(Context);

        SelectedTextKeepTime = TextKeepTimes.First(x => x.Value == Settings.TextKeepTime);
        SelectedImagesKeepTime = ImagesKeepTimes.First(x => x.Value == Settings.ImagesKeepTime);
        SelectedFilesKeepTime = FilesKeepTimes.First(x => x.Value == Settings.FilesKeepTime);

        OnPropertyChanged(nameof(TextKeepTimes));
        OnPropertyChanged(nameof(ImagesKeepTimes));
        OnPropertyChanged(nameof(FilesKeepTimes));
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
            ReloadDataAsync = null!;
            SaveSettings = null!;
            RecordOrders = null!;
            ClickActions = null!;
            TextKeepTimes = null!;
            ImagesKeepTimes = null!;
            FilesKeepTimes = null!;
        }
    }

    #endregion
}
