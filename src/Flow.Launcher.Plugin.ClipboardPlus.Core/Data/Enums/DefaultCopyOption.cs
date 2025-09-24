using Flow.Launcher.Localization.Attributes;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

[EnumLocalize]
public enum DefaultRichTextCopyOption
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_rich_text_title))]
    Rtf = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_plain_text_title))]
    Plain = 1
}

[EnumLocalize]
public enum DefaultImageCopyOption
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_image_title))]
    Image = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_image_file_title))]
    File = 1
}

[EnumLocalize]
public enum DefaultFilesCopyOption
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_files_title))]
    Files = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_sort_name_asc_title))]
    NameAsc = 1,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_sort_name_desc_title))]
    NameDesc = 2,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_file_path_title))]
    Path = 3,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_copy_file_content_title))]
    Content = 4
}
