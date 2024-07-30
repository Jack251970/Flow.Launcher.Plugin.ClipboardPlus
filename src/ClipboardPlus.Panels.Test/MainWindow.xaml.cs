using ClipboardPlus.Core.Data.Enums;
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
            SettingsPanel.KeepTextHours = RecordKeepTime.Hours24;
            SettingsPanel.KeepImageHours = RecordKeepTime.Year1;
            SettingsPanel.KeepFileHours = RecordKeepTime.Month1;
        }
    }
}
