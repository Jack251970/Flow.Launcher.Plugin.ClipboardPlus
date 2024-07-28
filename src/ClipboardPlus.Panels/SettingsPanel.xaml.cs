using System;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using ClipboardPlus.Core;

namespace ClipboardPlus.Panels;

public partial class SettingsPanel
{
    public Settings settings { get; set; }
    private PluginInitContext? _context { get; set; }
    private bool Ready { get; set; } = false;

    #region Dependency Properties

    public static readonly DependencyProperty ImageFormatStringProperty = DependencyProperty.Register(
        nameof(ImageFormatString), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string ImageFormatString
    {
        get { return (string)GetValue(ImageFormatStringProperty); }
        set { SetValue(ImageFormatStringProperty, value); }
    }
    public static readonly DependencyProperty MaxDataCountProperty = DependencyProperty.Register(
        nameof(MaxDataCount),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public static readonly DependencyProperty ImageFormatPreviewProperty = DependencyProperty.Register(
        nameof(ImageFormatPreview), typeof(string), typeof(SettingsPanel), new PropertyMetadata(default(string)));

    public string ImageFormatPreview
    {
        get { return (string)GetValue(ImageFormatPreviewProperty); }
        set { SetValue(ImageFormatPreviewProperty, value); }
    }

    public int MaxDataCount
    {
        get => settings.MaxDataCount;
        set
        {
            SetValue(MaxDataCountProperty, value);
            settings.MaxDataCount = value;
            SpinBoxMaxRec.Value = Convert.ToInt32(value);
        }
    }

    public static readonly DependencyProperty OrderByProperty = DependencyProperty.Register(
        nameof(OrderBy),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public int OrderBy
    {
        get => settings.OrderBy;
        set
        {
            SetValue(OrderByProperty, value);
            CmBoxOrderBy.SelectedIndex = value;
        }
    }

    public static readonly DependencyProperty KeepTextHoursProperty = DependencyProperty.Register(
        nameof(KeepTextHours),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public int KeepTextHours
    {
        get => settings.KeepTextHours;
        set
        {
            SetValue(KeepTextHoursProperty, value);
            CmBoxKeepText.SelectedIndex = value;
        }
    }

    public static readonly DependencyProperty KeepImageHoursProperty = DependencyProperty.Register(
        nameof(KeepImageHours),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public int KeepImageHours
    {
        get => settings.KeepImageHours;
        set
        {
            SetValue(KeepImageHoursProperty, value);
            CmBoxKeepImages.SelectedIndex = value;
        }
    }

    public static readonly DependencyProperty KeepFileHoursProperty = DependencyProperty.Register(
        nameof(KeepFileHours),
        typeof(int),
        typeof(SettingsPanel),
        new PropertyMetadata(default(int))
    );

    public int KeepFileHours
    {
        get => settings.KeepImageHours;
        set
        {
            SetValue(KeepFileHoursProperty, value);
            CmBoxKeepFiles.SelectedIndex = value;
        }
    }

    #endregion

    public SettingsPanel(Settings settings, PluginInitContext ctx)
    {
        this.settings = settings;
        _context = ctx;
        InitializeComponent();
        Ready = true;
        MaxDataCount = settings.MaxDataCount;
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        ImageFormatString = settings.ImageFormat;
        ImageFormatPreview = Utils.FormatImageName(ImageFormatString, DateTime.Now, "TestApp.exe");
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public SettingsPanel()
    {
        settings = new Settings() { ConfigFile = "test.json" };
        settings.Save();
        settings = Settings.Load("test.json");
        _context = null;
        InitializeComponent();
        Ready = true;
        MaxDataCount = settings.MaxDataCount;
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        OrderBy = settings.OrderBy;
        Console.WriteLine(settings);
    }

    #region Cache Image

    private void CkBoxCacheImages_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.CacheImages = true;
    }

    private void CkBoxCacheImages_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.CacheImages = false;
    }

    #endregion

    #region Apply Settings

    private void BtnApplySettings_OnClick(object sender, RoutedEventArgs e)
    {
        // _context?.API.RestartApp();
        _context?.API.SavePluginSettings();
        _context?.API.ReloadAllPluginData();
    }

    #endregion

    #region Max Records

    private void SpinBoxMaxRec_OnValueChanged(int v)
    {
        MaxDataCount = int.Max(v, 0);
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

    private void TextBoxImageFormat_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ImageFormatString = TextBoxImageFormat.Text;
        settings.ImageFormat = ImageFormatString;
        ImageFormatPreview = Utils.FormatImageName(ImageFormatString, DateTime.Now, "TestApp.exe");
    }

    #endregion

    #region Order By

    private void CmBoxOrderBy_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            settings.OrderBy = CmBoxOrderBy.SelectedIndex;
        }
    }

    #endregion

    #region Keep Text & Image & Files

    private void CkBoxKeepText_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.KeepText = true;
    }

    private void CkBoxKeepText_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.KeepText = false;
    }

    private void CmBoxKeepText_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            settings.KeepTextHours = CmBoxKeepText.SelectedIndex;
        }
    }

    private void CkBoxKeepImages_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.KeepImage = true;
    }

    private void CkBoxKeepImages_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.KeepImage = false;
    }

    private void CmBoxKeepImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            settings.KeepImageHours = CmBoxKeepImages.SelectedIndex;
        }
    }

    private void CkBoxKeepFiles_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.KeepFile = true;
    }

    private void CkBoxKeepFiles_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.KeepFile = false;
    }

    private void CmBoxKeepFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Ready)
        {
            settings.KeepFileHours = CmBoxKeepFiles.SelectedIndex;
        }
    }

    #endregion
}
