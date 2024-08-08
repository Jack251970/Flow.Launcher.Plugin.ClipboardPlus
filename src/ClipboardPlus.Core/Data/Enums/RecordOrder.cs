using System.ComponentModel;

namespace ClipboardPlus.Core.Data.Enums;

public enum RecordOrder
{
    [Description("flowlauncher_plugin_clipboardplus_record_order_score")]
    Score = 0,

    [Description("flowlauncher_plugin_clipboardplus_record_order_create_time")]
    CreateTime = 1,

    [Description("flowlauncher_plugin_clipboardplus_record_order_source_application")]
    SourceApplication = 2,

    [Description("flowlauncher_plugin_clipboardplus_record_order_data_type")]
    DataType = 3,
}
