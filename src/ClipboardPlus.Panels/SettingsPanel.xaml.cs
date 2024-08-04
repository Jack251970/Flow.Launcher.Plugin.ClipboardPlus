using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipboardPlus.Panels;

public partial class SettingsPanel : UserControl
{
    public Settings Settings { get; set; }
    private PluginInitContext? Context { get; set; }
    private bool Ready { get; set; } = false;

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

    public static readonly DependencyProperty ImageFormatStringProperty = DependencyProperty.Register(
        nameof(ImageFormatString), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string ImageFormatString
    {
        get { return (string)GetValue(ImageFormatStringProperty); }
        set { SetValue(ImageFormatStringProperty, value); }
    }

    public static readonly DependencyProperty ImageFormatPreviewProperty = DependencyProperty.Register(
        nameof(ImageFormatPreview), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string ImageFormatPreview
    {
        get { return (string)GetValue(ImageFormatPreviewProperty); }
        set { SetValue(ImageFormatPreviewProperty, value); }
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

    public SettingsPanel(Settings settings, PluginInitContext context)
    {
        Settings = settings;
        Context = context;
        InitializeComponent();
        PathHelpers.Init(context);
        ClearKeywordString = settings.ClearKeyword;
        MaxDataCount = settings.MaxDataCount;
        OrderBy = settings.OrderBy;
        ImageFormatString = settings.ImageFormat;
        ImageFormatPreview = GetImageFormatPreview();
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
        Context = null;
        InitializeComponent();
        ClearKeywordString = settings.ClearKeyword;
        MaxDataCount = settings.MaxDataCount;
        OrderBy = settings.OrderBy;
        ImageFormatString = settings.ImageFormat;
        ImageFormatPreview = GetImageFormatPreview();
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        Console.WriteLine(settings);
        Ready = true;
    }

    // TODO: Fix fast-multi-time saving clash.
    private void ApplySettings()
    {
        Context?.API.SavePluginSettings();
        Context?.API.ReloadAllPluginData();
    }

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

    #region Image Format

    private void ButtonYear_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "yyyy";
    }

    private void ButtonMonth_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "MM";
    }

    private void ButtonDay_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "dd";
    }

    private void ButtonHour_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "hh";
    }

    private void ButtonMinute_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "mm";
    }

    private void ButtonSecond_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "ss";
    }

    private void ButtonAppName_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxImageFormat.Text += "{app}";
    }

    #endregion

    #region Preview Format

    private string GetImageFormatPreview()
    {
        return StringUtils.FormatImageName(ImageFormatString, DateTime.Now, "Flow.Launcher.exe");
    }

    private void TextBoxImageFormat_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ImageFormatString = TextBoxImageFormat.Text;
        Settings.ImageFormat = ImageFormatString;
        ImageFormatPreview = GetImageFormatPreview();
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
            ApplySettings();
        }
    }

    private void CmBoxKeepText_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepTextHours = (RecordKeepTime)CmBoxKeepText.SelectedIndex;
            ApplySettings();
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
            ApplySettings();
        }
    }

    private void CmBoxKeepImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepImageHours = (RecordKeepTime)CmBoxKeepImages.SelectedIndex;
            ApplySettings();
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
            ApplySettings();
        }
    }

    private void CmBoxKeepFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            Settings.KeepFileHours = (RecordKeepTime)CmBoxKeepFiles.SelectedIndex;
            ApplySettings();
        }
    }

    #endregion
}
