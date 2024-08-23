using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class AutoSelectTextBox : TextBox
{
    public AutoSelectTextBox()
    {
        Focusable = true;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        IsReadOnly = false;
        IsUndoEnabled = true;
        TextAlignment = TextAlignment.Left;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        SelectAll();
    }
}
