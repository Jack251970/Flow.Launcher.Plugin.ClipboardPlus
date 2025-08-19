using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        ViewModel.InitializeContent();
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

    #region Wrap Text

    private void PlainTextScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ScrollViewer plainTextScrollViewer)
            return;

        SetTextBoxWidth(plainTextScrollViewer, PlainTextBox);
    }

    private void RichTextScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ScrollViewer richTextScrollViewer)
            return;

        SetTextBoxWidth(richTextScrollViewer, RichTextBox);
    }

    private static void SetTextBoxWidth(ScrollViewer scrollViewer, Control textBox)
    {
        var actualWidth = scrollViewer.ActualWidth;
        if (double.IsNaN(actualWidth))
            return;

        var padding = scrollViewer.Padding.Left + scrollViewer.Padding.Right;
        var margin = textBox.Margin.Left + textBox.Margin.Right;
        var dpi = VisualTreeHelper.GetDpi(scrollViewer);
        var scale = dpi.DpiScaleX;
        var deviation = (padding + margin) * scale;
        var richTextBoxWidth = Math.Max(0, actualWidth - deviation);

        textBox.Width = richTextBoxWidth;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        ViewModel = null!;
    }

    #endregion
}
