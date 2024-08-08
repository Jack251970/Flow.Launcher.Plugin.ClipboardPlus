using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ClipboardPlus.Panels.Views;

public partial class SettingsPanel : UserControl
{
    #region Properties

    // View model
    private readonly SettingsViewModel ViewModel;

    #endregion

    #region Constructors

    public SettingsPanel(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public SettingsPanel()
    {
        var settings = new Settings();
        var viewModel = new SettingsViewModel(null!, settings, null!, null!);
        DataContext = viewModel;
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    #endregion

    #region Cache Image Button

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

    #region Cache Format Buttons

    private void FormatButton_OnClick(object sender, RoutedEventArgs e)
    {
        var customString = ((Button)sender).Tag.ToString();
        var cursorIndex = TextBoxCacheFormat.CaretIndex;
        TextBoxCacheFormat.Text = TextBoxCacheFormat.Text.Insert(cursorIndex, customString ?? string.Empty);
    }

    #endregion
}
