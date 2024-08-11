using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class PreviewPanel : UserControl
{
    public readonly PreviewViewModel ViewModel;

    public PreviewPanel(IClipboardPlus clipboardPlus, ClipboardData clipboardData)
    {
        ViewModel = new PreviewViewModel(clipboardPlus, clipboardData);
        DataContext = ViewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}
