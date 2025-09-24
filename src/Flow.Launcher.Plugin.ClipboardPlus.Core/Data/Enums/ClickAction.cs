using Flow.Launcher.Localization.Attributes;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

[EnumLocalize]
public enum ClickAction
{
    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy))]
    Copy = 0,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy_paste))]
    CopyPaste = 1,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy_delete_list))]
    CopyDeleteList = 2,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy_delete_both))]
    CopyDeleteListDatabase = 3,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy_paste_delete_list))]
    CopyPasteDeleteList = 4,

    [EnumLocalizeKey(nameof(Localize.flowlauncher_plugin_clipboardplus_click_action_copy_paste_delete_both))]
    CopyPasteDeleteListDatabase = 5,
}
