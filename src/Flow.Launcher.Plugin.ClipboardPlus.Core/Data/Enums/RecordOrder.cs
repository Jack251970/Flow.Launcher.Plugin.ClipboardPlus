using Flow.Launcher.Localization.Attributes;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

[EnumLocalize]
public enum RecordOrder
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_order_create_time))]
    CreateTime = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_order_source_application))]
    SourceApplication = 1,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_record_order_data_type))]
    DataType = 2,
}
