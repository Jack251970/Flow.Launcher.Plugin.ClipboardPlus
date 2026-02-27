using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class SettingsPanel : UserControl
{
    public readonly SettingsViewModel ViewModel;
    private readonly IClipboardPlus _clipboardPlus;

    public SettingsPanel(IClipboardPlus clipboardPlus)
    {
        ViewModel = new SettingsViewModel(clipboardPlus);
        _clipboardPlus = clipboardPlus;
        DataContext = ViewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ListView listView) return;
        if (listView.View is not GridView gridView) return;

        var workingWidth =
            listView.ActualWidth - SystemParameters.VerticalScrollBarWidth; // take into account vertical scrollbar

        if (workingWidth <= 0) return;

        var col1 = 1.00;

        gridView.Columns[0].Width = workingWidth * col1;
    }

    private void DeleteProgramSource_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedItems = ProgramSourceView
            .SelectedItems.Cast<AppInfo>()
            .ToList();

        var itemsToRemove = _clipboardPlus.Settings.ExcludedApps
            .Where(app => selectedItems.Any(selected => selected.Equals(app)))
            .ToList();
        foreach (var item in itemsToRemove)
        {
            var index = _clipboardPlus.Settings.ExcludedApps.IndexOf(item);
            _clipboardPlus.Settings.ExcludedApps.RemoveAt(index);
        }

        ProgramSourceView.SelectedItems.Clear();
    }

    private void AddProgramSource_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var dialog = new AppSelectionWindow(_clipboardPlus.Settings.ExcludedApps)
        {
            Owner = Window.GetWindow(button)
        };

        var result = dialog.ShowDialog();
        if (result == true && dialog.ViewModel.SelectedApp != null)
        {
            var addedApp = dialog.ViewModel.SelectedApp;

            // Check if app already exists in the list (additional safety check)
            var existingApp = _clipboardPlus.Settings.ExcludedApps.FirstOrDefault(a =>
                a.Equals(addedApp));

            if (existingApp == null)
            {
                _clipboardPlus.Settings.ExcludedApps.Add(addedApp);
                ProgramSourceView.SelectedItems.Clear(); // Clear selection after adding
            }
        }
    }
}
