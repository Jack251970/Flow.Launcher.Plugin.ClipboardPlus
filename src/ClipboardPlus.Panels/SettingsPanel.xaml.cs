using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipboardPlus.Panels;

public partial class SettingsPanel : UserControl
{
    #region Properties

    // Settings
    public Settings Settings { get; set; }

    // Callbacks
    private Func<Task>? ReloadDataAsync { get; set; }
    private Action? Save { get; set; }

    // Initial state
    private bool Ready { get; set; } = false;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty ClearKeywordStringProperty = DependencyProperty.Register(
        nameof(ClearKeywordString), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string ClearKeywordString
    {
        get { return (string)GetValue(ClearKeywordStringProperty); }
        set { SetValue(ClearKeywordStringProperty, value); }
    }

    public static readonly DependencyProperty MaxDataCountProperty = DependencyProperty.Register(
        nameof(MaxDataCount),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public int MaxDataCount
    {
        get => Settings.MaxDataCount;
        set
        {
            SetValue(MaxDataCountProperty, value);
            Settings.MaxDataCount = value;
            MaxRecValueBox.Text = value.ToString();
        }
    }

    public static readonly DependencyProperty OrderByProperty = DependencyProperty.Register(
        nameof(OrderBy),
        typeof(CbOrders),
        typeof(SettingsPanel),
        new PropertyMetadata(default(CbOrders))
    );

    public CbOrders OrderBy
    {
        get => Settings.OrderBy;
        set
        {
            SetValue(OrderByProperty, value);
            CmBoxOrderBy.SelectedIndex = (int)value;
        }
    }

    public static readonly DependencyProperty CacheFormatStringProperty = DependencyProperty.Register(
        nameof(CacheFormatString), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string CacheFormatString
    {
        get { return (string)GetValue(CacheFormatStringProperty); }
        set { SetValue(CacheFormatStringProperty, value); }
    }

    public static readonly DependencyProperty CacheFormatPreviewProperty = DependencyProperty.Register(
        nameof(CacheFormatPreview), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string CacheFormatPreview
    {
        get { return (string)GetValue(CacheFormatPreviewProperty); }
        set { SetValue(CacheFormatPreviewProperty, value); }
    }

    public static readonly DependencyProperty KeepTextHoursProperty = DependencyProperty.Register(
        nameof(KeepTextHours),
        typeof(RecordKeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(RecordKeepTime))
    );

    public RecordKeepTime KeepTextHours
    {
        get => Settings.KeepTextHours;
        set
        {
            SetValue(KeepTextHoursProperty, value);
            CmBoxKeepText.SelectedIndex = (int)value;
        }
    }

    public static readonly DependencyProperty KeepImageHoursProperty = DependencyProperty.Register(
        nameof(KeepImageHours),
        typeof(RecordKeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(RecordKeepTime))
    );

    public RecordKeepTime KeepImageHours
    {
        get => Settings.KeepImageHours;
        set
        {
            SetValue(KeepImageHoursProperty, value);
            CmBoxKeepImages.SelectedIndex = (int)value;
        }
    }

    public static readonly DependencyProperty KeepFileHoursProperty = DependencyProperty.Register(
        nameof(KeepFileHours),
        typeof(RecordKeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(RecordKeepTime))
    );

    public RecordKeepTime KeepFileHours
    {
        get => Settings.KeepImageHours;
        set
        {
            SetValue(KeepFileHoursProperty, value);
            CmBoxKeepFiles.SelectedIndex = (int)value;
        }
    }

    #endregion

    #region Constructors

    public SettingsPanel(Settings settings, PluginInitContext context, Func<Task>? reloadDataAsync, Action? save)
    {
        Settings = settings;
        ReloadDataAsync = reloadDataAsync;
        Save = save;
        InitializeComponent();
        PathHelpers.Init(context);
        ClearKeywordString = settings.ClearKeyword;
        MaxDataCount = settings.MaxDataCount;
        OrderBy = settings.OrderBy;
        CacheFormatString = settings.CacheFormat;
        CacheFormatPreview = GetCacheFormatPreview();
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        Ready = true;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public SettingsPanel()
    {
        var settings = new Settings();
        Settings = settings;
        InitializeComponent();
        ClearKeywordString = settings.ClearKeyword;
        MaxDataCount = settings.MaxDataCount;
        OrderBy = settings.OrderBy;
        CacheFormatString = settings.CacheFormat;
        CacheFormatPreview = GetCacheFormatPreview();
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        Console.WriteLine(settings);
        Ready = true;
    }

    #endregion

    #region Settings

    private void ApplySettings(bool reload = false)
    {
        Save?.Invoke();
        if (reload)
        {
            ReloadDataAsync?.Invoke();
        }
    }

    #endregion

    #region Clear Keyword

    private void TextBoxClearKeyword_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.ClearKeyword = TextBoxClearKeyword.Text;
            ApplySettings();
        }
    }

    #endregion

    #region Max Records

    private readonly int MaxRecMaximumValue = 100000;
    private bool isUpdatingText = false;

    private void MaxRecValueBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = NumberRegex().IsMatch(e.Text);
        if (int.TryParse(e.Text, out int a))
        {
            e.Handled = e.Handled && a <= MaxRecMaximumValue;
        }
    }

    private void MaxRecValueBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isUpdatingText)
        {
            return;
        }

        if (Ready && (!string.IsNullOrEmpty(MaxRecValueBox.Text)) && int.TryParse(MaxRecValueBox.Text, out var v))
        {
            isUpdatingText = true;
            MaxDataCount = (v > MaxRecMaximumValue) ? MaxRecMaximumValue : Math.Max(v, 0);
            isUpdatingText = false;
            ApplySettings();
        }
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();

    #endregion

    #region Order By

    private void CmBoxOrderBy_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.OrderBy = (CbOrders)CmBoxOrderBy.SelectedIndex;
            ApplySettings();
        }
    }

    #endregion

    #region Click Action

    private void CmBoxClickAction_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.ClickAction = (ClickAction)CmBoxClickAction.SelectedIndex;
            ApplySettings();
        }
    }

    #endregion

    #region Cache Image

    private void CkBoxCacheImages_OnChecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.CacheImages = true;
            ApplySettings();
        }
    }

    private void CkBoxCacheImages_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.CacheImages = false;
            ApplySettings();
        }
    }

    private void CacheImageButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = PathHelpers.ImageCachePath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    #endregion

    #region Cache Format

    private void ButtonYear_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "yyyy";
    }

    private void ButtonMonth_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "MM";
    }

    private void ButtonDay_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "dd";
    }

    private void ButtonHour_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "hh";
    }

    private void ButtonMinute_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "mm";
    }

    private void ButtonSecond_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "ss";
    }

    private void ButtonApp_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCacheFormat.Text += "{app}";
    }

    #endregion

    #region Cache Format Preview

    private string GetCacheFormatPreview()
    {
        return StringUtils.FormatImageName(CacheFormatString, DateTime.Now, "Flow.Launcher.exe");
    }

    private void TextBoxCacheFormat_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CacheFormatString = TextBoxCacheFormat.Text;
        Settings.CacheFormat = CacheFormatString;
        CacheFormatPreview = GetCacheFormatPreview();
        if (Ready)
        {
            ApplySettings();
        }
    }

    #endregion

    #region Keep Text

    private void CkBoxKeepText_OnChecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepText = true;
            ApplySettings();
        }
    }

    private void CkBoxKeepText_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepText = false;
            ApplySettings(true);
        }
    }

    private void CmBoxKeepText_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepTextHours = (RecordKeepTime)CmBoxKeepText.SelectedIndex;
            ApplySettings(true);
        }
    }

    #endregion

    #region Keep Images

    private void CkBoxKeepImages_OnChecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepImage = true;
            ApplySettings();
        }
    }

    private void CkBoxKeepImages_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepImage = false;
            ApplySettings(true);
        }
    }

    private void CmBoxKeepImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepImageHours = (RecordKeepTime)CmBoxKeepImages.SelectedIndex;
            ApplySettings(true);
        }
    }

    #endregion

    #region Keep Files

    private void CkBoxKeepFiles_OnChecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepFile = true;
            ApplySettings();
        }
    }

    private void CkBoxKeepFiles_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepFile = false;
            ApplySettings(true);
        }
    }

    private void CmBoxKeepFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepFileHours = (RecordKeepTime)CmBoxKeepFiles.SelectedIndex;
            ApplySettings(true);
        }
    }

    #endregion
}
