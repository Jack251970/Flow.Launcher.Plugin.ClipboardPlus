using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class SettingsPanel : UserControl
{
    public readonly SettingsViewModel ViewModel;

    public SettingsPanel(IClipboardPlus clipboardPlus)
    {
        ViewModel = new SettingsViewModel(clipboardPlus);
        DataContext = ViewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}
