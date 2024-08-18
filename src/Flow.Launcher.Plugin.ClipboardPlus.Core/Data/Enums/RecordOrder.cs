using System.ComponentModel;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

public enum RecordOrder
{
    [Description("flowlauncher_plugin_clipboardplus_record_order_create_time")]
    CreateTime = 0,

    [Description("flowlauncher_plugin_clipboardplus_record_order_source_application")]
    SourceApplication = 1,

    [Description("flowlauncher_plugin_clipboardplus_record_order_data_type")]
    DataType = 2,
}
