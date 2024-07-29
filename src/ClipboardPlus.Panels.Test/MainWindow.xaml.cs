using ClipboardPlus.Core;
using System.Windows;

namespace ClipboardPlus.Panels.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SettingsPanel.MaxDataCount = 300;
            SettingsPanel.KeepTextHours = KeepTime.Hours24;
            SettingsPanel.KeepImageHours = KeepTime.Year1;
            SettingsPanel.KeepFileHours = KeepTime.Month1;
        }
    }
}
