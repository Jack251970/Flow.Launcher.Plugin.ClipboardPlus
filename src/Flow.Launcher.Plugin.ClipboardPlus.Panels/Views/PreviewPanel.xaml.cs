using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class PreviewPanel : UserControl
{
    public readonly PreviewViewModel ViewModel;

    public PreviewPanel(PluginInitContext context, ClipboardData clipboardData)
    {
        ViewModel = new PreviewViewModel(context, clipboardData);
        DataContext = ViewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}
