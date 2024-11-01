using System.ComponentModel;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

public enum DefaultRichTextCopyOption
{
    [Description("flowlauncher_plugin_clipboardplus_copy_rich_text_title")]
    Rtf = 0,

    [Description("flowlauncher_plugin_clipboardplus_copy_plain_text_title")]
    Plain = 1
}

public enum DefaultImageCopyOption
{
    [Description("flowlauncher_plugin_clipboardplus_copy_image_title")]
    Image = 0,

    [Description("flowlauncher_plugin_clipboardplus_copy_image_file_title")]
    File = 1
}

public enum DefaultFilesCopyOption
{
    [Description("flowlauncher_plugin_clipboardplus_copy_files_title")]
    Files = 0,

    [Description("flowlauncher_plugin_clipboardplus_copy_sort_name_asc_title")]
    NameAsc = 1,

    [Description("flowlauncher_plugin_clipboardplus_copy_sort_name_desc_title")]
    NameDesc = 2,

    [Description("flowlauncher_plugin_clipboardplus_copy_file_path_title")]
    Path = 3,

    [Description("flowlauncher_plugin_clipboardplus_copy_file_content_title")]
    Content = 4
}
