using System;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Views;

public partial class PreviewPanel : UserControl, IDisposable
{
    public PreviewViewModel ViewModel;

    public PreviewPanel(IClipboardPlus clipboardPlus, ClipboardData clipboardData)
    {
        ViewModel = new PreviewViewModel(clipboardPlus, clipboardData);
        DataContext = ViewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    #region Auto Select

    private async void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        });
    }

    private async void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (sender is RichTextBox richTextBox)
            {
                richTextBox.SelectAll();
            }
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        ViewModel = null!;
    }

    #endregion
}
