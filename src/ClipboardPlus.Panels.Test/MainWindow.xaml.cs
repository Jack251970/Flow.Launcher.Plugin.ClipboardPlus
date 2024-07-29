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
            // var settings = new Settings();
            // settings.MaxDataCount = 300;
            // settings.KeepText = true;
            // settings.KeepImage = true;
            // settings.KeepFile = true;
            // settings.KeepTextHours = 3;
            // settings.KeepImageHours = 2;
            // settings.KeepFileHours = 2;
            // SettingsPanel.settings = settings;
            // SettingsPanel.MaxDataCount = settings.MaxDataCount;
            //
            SettingsPanel.KeepTextHours = 1;
        }
    }
}
