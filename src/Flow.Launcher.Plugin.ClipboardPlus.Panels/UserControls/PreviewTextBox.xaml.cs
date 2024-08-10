using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.UserControls;

public partial class PreviewTextBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(PreviewTextBox),
        new PropertyMetadata(default(string), OnTextPropertyChanged)
    );

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (PreviewTextBox)d;
        control.UpdateTextBoxValue((string)e.NewValue);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set
        {
            SetValue(TextProperty, value);
        }
    }

    public PreviewTextBox()
    {
        InitializeComponent();
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(TextBox.SelectAll);
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = TextBox.Text;
    }

    private void UpdateTextBoxValue(string newValue)
    {
        if (TextBox.Text != newValue)
        {
            TextBox.Text = newValue;
        }
    }
}
