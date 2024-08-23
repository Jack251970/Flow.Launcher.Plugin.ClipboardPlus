using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class AutoSelectRichTextBox : RichTextBox
{
    public AutoSelectRichTextBox()
    {
        Focusable = true;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        IsReadOnly = false;
        IsUndoEnabled = true;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        GotFocus += RichTextBox_GotFocus;
    }

    private void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(SelectAll);
    }
}
