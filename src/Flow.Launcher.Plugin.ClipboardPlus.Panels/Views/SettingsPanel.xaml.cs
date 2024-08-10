using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class SettingsPanel : UserControl
{
    public readonly SettingsViewModel ViewModel;

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
        var viewModel = new SettingsViewModel(null!, settings);
        DataContext = viewModel;
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }
}
