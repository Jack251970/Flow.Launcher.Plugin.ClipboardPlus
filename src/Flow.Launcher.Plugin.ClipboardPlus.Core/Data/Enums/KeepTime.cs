using System.ComponentModel;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

public enum KeepTime
{
    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_always")]
    Always = 0,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_1_hour")]
    Hour1 = 1,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_12_hours")]
    Hours12 = 2,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_24_hours")]
    Hours24 = 3,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_3_days")]
    Days3 = 4,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_7_days")]
    Days7 = 5,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_1_month")]
    Month1 = 6,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_3_months")]
    Months6 = 7,

    [Description("flowlauncher_plugin_clipboardplus_record_keep_time_1_year")]
    Year1 = 8,
}
