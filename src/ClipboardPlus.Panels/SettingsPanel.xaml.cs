using ClipboardPlus.Core;
using Flow.Launcher.Plugin;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels;

public partial class SettingsPanel
{
    public Settings Settings { get; set; }
    private PluginInitContext? Context { get; set; }
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
        get => Settings.MaxDataCount;
        set
        {
            SetValue(MaxDataCountProperty, value);
            Settings.MaxDataCount = value;
            SpinBoxMaxRec.Value = Convert.ToInt32(value);
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

    public static readonly DependencyProperty KeepTextHoursProperty = DependencyProperty.Register(
        nameof(KeepTextHours),
        typeof(KeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(KeepTime))
    );

    public KeepTime KeepTextHours
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
        typeof(KeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(KeepTime))
    );

    public KeepTime KeepImageHours
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
        typeof(KeepTime),
        typeof(SettingsPanel),
        new PropertyMetadata(default(KeepTime))
    );

    public KeepTime KeepFileHours
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
        MaxDataCount = settings.MaxDataCount;
        OrderBy = settings.OrderBy;
        KeepTextHours = settings.KeepTextHours;
        KeepImageHours = settings.KeepImageHours;
        KeepFileHours = settings.KeepFileHours;
        ImageFormatString = settings.ImageFormat;
        ImageFormatPreview = Utils.FormatImageName(ImageFormatString, DateTime.Now, "TestApp.exe");
        Ready = true;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public SettingsPanel()
    {
        Settings = new Settings() { ConfigFile = "test.json" };
        Settings.Save();
        Settings = Settings.Load("test.json");
        Context = null;
        InitializeComponent();
        MaxDataCount = Settings.MaxDataCount;
        OrderBy = Settings.OrderBy;
        KeepTextHours = Settings.KeepTextHours;
        KeepImageHours = Settings.KeepImageHours;
        KeepFileHours = Settings.KeepFileHours;
        ImageFormatString = Settings.ImageFormat;
        ImageFormatPreview = Utils.FormatImageName(ImageFormatString, DateTime.Now, "TestApp.exe");
        Console.WriteLine(Settings);
        Ready = true;
    }

    private void ApplySettings()
    {
        Context?.API.SavePluginSettings();
        Context?.API.ReloadAllPluginData();
    }

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

    #endregion

    #region Max Records

    private void SpinBoxMaxRec_OnValueChanged(int v)
    {
        if (Ready)
        {
            MaxDataCount = int.Max(v, 0);
            ApplySettings();
        }
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
        Settings.ImageFormat = ImageFormatString;
        ImageFormatPreview = Utils.FormatImageName(ImageFormatString, DateTime.Now, "TestApp.exe");
        if (Ready)
        {
            ApplySettings();
        }
    }

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
            Settings.KeepTextHours = (KeepTime)CmBoxKeepText.SelectedIndex;
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
            Settings.KeepImageHours = (KeepTime)CmBoxKeepImages.SelectedIndex;
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
            Settings.KeepFileHours = (KeepTime)CmBoxKeepFiles.SelectedIndex;
            ApplySettings();
        }
    }

    #endregion
}
