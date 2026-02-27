using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views
{
    public partial class AppSelectionWindow : Window
    {
        public AppSelectionViewModel ViewModel { get; }

        public AppSelectionWindow()
        {
            InitializeComponent();
            ViewModel = new AppSelectionViewModel();
            DataContext = ViewModel;
        }

        public AppSelectionWindow(IEnumerable<AppInfo> existingApps)
        {
            InitializeComponent();
            ViewModel = new AppSelectionViewModel(existingApps);
            DataContext = ViewModel;
        }

        private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = ViewModel.SelectedApp != null;
            Close();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AppListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedApp != null)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
