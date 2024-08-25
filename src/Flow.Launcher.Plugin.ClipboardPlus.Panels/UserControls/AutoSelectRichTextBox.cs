using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.UserControls;

public class AutoSelectRichTextBox : RichTextBox
{
    public static readonly DependencyProperty UnicodeTextProperty = DependencyProperty.Register(
        nameof(UnicodeText),
        typeof(string),
        typeof(AutoSelectRichTextBox),
        new PropertyMetadata(default(string), OnUnicodeTextPropertyChanged)
    );

    private static void OnUnicodeTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        RichTextBox rtb = (RichTextBox)d;
        rtb.SetUnicodeText((string)e.NewValue);
    }

    public string UnicodeText
    {
        get => (string)GetValue(UnicodeTextProperty);
        set { SetValue(UnicodeTextProperty, value); }
    }

    public static readonly DependencyProperty RichTextProperty = DependencyProperty.Register(
        nameof(RichText),
        typeof(string),
        typeof(AutoSelectRichTextBox),
        new PropertyMetadata(default(string), OnRichTextPropertyChanged)
    );

    private static void OnRichTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        RichTextBox rtb = (RichTextBox)d;
        rtb.SetRichText((string)e.NewValue);
    }

    public string RichText
    {
        get => (string)GetValue(RichTextProperty);
        set { SetValue(RichTextProperty, value); }
    }

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
