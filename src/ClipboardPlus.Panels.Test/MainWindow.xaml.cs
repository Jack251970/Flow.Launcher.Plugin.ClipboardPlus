using ClipboardPlus.Panels.Views;
using System.Windows;

namespace ClipboardPlus.Panels.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.ViewModel.OnCultureInfoChanged(null!);
    }
}
