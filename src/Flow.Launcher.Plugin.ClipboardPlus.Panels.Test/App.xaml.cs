using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

public partial class App : Application
{
    public App()
    {
        AddResources();
        InitializeComponent();
    }

    private void AddResources()
    {
        // Colors
        var color03B = new SolidColorBrush(Color.FromRgb(29, 29, 29));
        Resources.Add("Color03B", color03B);
        var color04B = new SolidColorBrush(Color.FromRgb(207, 207, 207));
        Resources.Add("Color04B", color04B);

        // Settings panel resources
        var settingsPanelMargin = new Thickness(70, 13.5, 18, 13.5);
        var settingsPanelItemLeftMargin = new Thickness(9, 0, 0, 0);
        var settingsPanelItemRightMargin = new Thickness(0, 0, 9, 0);
        var settingsPanelItemTopBottomMargin = new Thickness(0, 4.5, 0, 4.5);
        var settingsPanelItemLeftTopBottomMargin = new Thickness(9, 4.5, 0, 4.5);
        var settingsPanelItemRightTopBottomMargin = new Thickness(0, 4.5, 9, 4.5);
        var settingsPanelSeparatorStyle = new Style(typeof(Separator))
        {
            Setters =
            {
                new Setter(Separator.MarginProperty, new Thickness(-70, 13.5, -18, 13.5)),
                new Setter(Separator.HeightProperty, 1d),
                new Setter(Separator.VerticalAlignmentProperty, VerticalAlignment.Top),
                new Setter(Separator.BackgroundProperty, FindResource("Color03B"))
            }
        };
        var settingsPanelTextBoxMinWidth = 180d;
        var settingsPanelPathTextBoxWidth = 240d;
        var settingsPanelAreaTextBoxMinHeight = 150d;
        var settingsPanelTextBlockDescriptionStyle = new Style(typeof(TextBlock))
        {
            Setters =
            {
                new Setter(TextBlock.FontSizeProperty, 12d),
                new Setter(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0)),
                new Setter(TextBlock.ForegroundProperty, FindResource("Color04B"))
            }
        };

        Resources.Add("SettingPanelMargin", settingsPanelMargin);
        Resources.Add("SettingPanelItemLeftMargin", settingsPanelItemLeftMargin);
        Resources.Add("SettingPanelItemRightMargin", settingsPanelItemRightMargin);
        Resources.Add("SettingPanelItemTopBottomMargin", settingsPanelItemTopBottomMargin);
        Resources.Add("SettingPanelItemLeftTopBottomMargin", settingsPanelItemLeftTopBottomMargin);
        Resources.Add("SettingPanelItemRightTopBottomMargin", settingsPanelItemRightTopBottomMargin);
        Resources.Add("SettingPanelSeparatorStyle", settingsPanelSeparatorStyle);
        Resources.Add("SettingPanelTextBoxMinWidth", settingsPanelTextBoxMinWidth);
        Resources.Add("SettingPanelPathTextBoxWidth", settingsPanelPathTextBoxWidth);
        Resources.Add("SettingPanelAreaTextBoxMinHeight", settingsPanelAreaTextBoxMinHeight);
        Resources.Add("SettingPanelTextBlockDescriptionStyle", settingsPanelTextBlockDescriptionStyle);
    }
}
