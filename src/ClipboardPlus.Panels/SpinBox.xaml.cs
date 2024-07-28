using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipboardPlus.Panels;

public partial class SpinBox : UserControl
{
    private static readonly Regex NumRegex = new Regex("[^0-9]+");
    public static readonly DependencyProperty PrefixTextProperty = DependencyProperty.Register(
        nameof(PrefixText),
        typeof(string),
        typeof(SpinBox),
        new PropertyMetadata(default(string))
    );

    public string PrefixText
    {
        get => (string)GetValue(PrefixTextProperty);
        set => SetValue(PrefixTextProperty, value);
    }

    public static readonly DependencyProperty SpinnerMaxProperty = DependencyProperty.Register(
        nameof(SpinnerMax),
        typeof(int),
        typeof(SpinBox),
        new PropertyMetadata(default(int))
    );

    public int SpinnerMax
    {
        get => (int)SpinnerScr.Maximum;
        set => SetValue(SpinnerMaxProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(SpinBox),
        new PropertyMetadata(default(int))
    );

    public int Value
    {
        get => (int)SpinnerScr.Value;
        set
        {
            SetValue(ValueProperty, value);
            SpinnerScr.Value = value;
        }
    }

    public delegate void OnValueChanged(int v);
    public event OnValueChanged? ValueChanged;

    public SpinBox(string prefixText)
    {
        PrefixText = prefixText;
        InitializeComponent();
    }

    public SpinBox()
    {
        PrefixText = "";
        InitializeComponent();
    }

    private void ValueBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = NumRegex.IsMatch(e.Text);
        if (int.TryParse(e.Text, out int a))
            e.Handled = e.Handled && a <= SpinnerScr.Maximum;
    }

    private void ValueBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(ValueBox.Text))
            return;
        if (!int.TryParse(ValueBox.Text, out var v))
            return;
        if (v > SpinnerScr.Maximum)
            ValueBox.Text = $"{SpinnerScr.Maximum}";
        SpinnerScr.Value = v;
        ValueChanged?.Invoke((int)SpinnerScr.Value);
    }
}
