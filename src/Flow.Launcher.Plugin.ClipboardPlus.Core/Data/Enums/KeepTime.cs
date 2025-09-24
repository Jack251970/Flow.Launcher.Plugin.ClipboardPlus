using Flow.Launcher.Localization.Attributes;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

[EnumLocalize]
public enum KeepTime
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_always))]
    Always = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_1_hour))]
    Hour1 = 1,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_12_hours))]
    Hours12 = 2,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_24_hours))]
    Hours24 = 3,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_3_days))]
    Days3 = 4,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_7_days))]
    Days7 = 5,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_1_month))]
    Month1 = 6,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_3_months))]
    Months6 = 7,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_keep_time_1_year))]
    Year1 = 8,
}
