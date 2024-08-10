using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.UserControls;

public partial class NumberBox : UserControl
{
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(int),
        typeof(NumberBox),
        new PropertyMetadata(0)
    );

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set
        {
            SetValue(MinimumProperty, value);
        }
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(int),
        typeof(NumberBox),
        new PropertyMetadata(100)
    );

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set
        {
            SetValue(MaximumProperty, value);
        }
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(NumberBox),
        new PropertyMetadata(default(int), OnValuePropertyChanged)
    );

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumberBox)d;
        control.UpdateTextBoxValue((int)e.NewValue);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set
        {
            SetValue(ValueProperty, value);
        }
    }

    public delegate void OnValueChanged(int value);
    public event OnValueChanged? ValueChanged;

    public NumberBox()
    {
        InitializeComponent();
    }

    private void ValueBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (NumberRegex().IsMatch(e.Text))
        {
            e.Handled = true;
            return;
        }
    }

    private void ValueBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(ValueBox.Text))
        {
            return;
        }

        if (!int.TryParse(ValueBox.Text, out var v))
        {
            return;
        }

        if (v <= Minimum)
        {
            ValueBox.Text = $"{Minimum}";
            Value = Minimum;
        }
        else if (v >= Maximum)
        {
            ValueBox.Text = $"{Maximum}";
            Value = Maximum;
        }
        else
        {
            Value = v;
        }

        ValueChanged?.Invoke(v);
    }

    private void UpdateTextBoxValue(int newValue)
    {
        if (ValueBox.Text != newValue.ToString())
        {
            ValueBox.Text = newValue.ToString();
        }
    }

    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();
}
