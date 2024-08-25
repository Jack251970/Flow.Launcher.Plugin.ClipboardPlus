using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.UserControls;

public class AutoSelectRichTextBox : RichTextBox
{
    public AutoSelectRichTextBox()
    {
        Focusable = true;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        IsReadOnly = false;
        IsUndoEnabled = true;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        SelectAll();
    }
}
