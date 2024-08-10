using System.ComponentModel;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Enums;

public enum ClickAction
{
    [Description("flowlauncher_plugin_clipboardplus_click_action_copy")]
    Copy = 0,

    [Description("flowlauncher_plugin_clipboardplus_click_action_copy_paste")]
    CopyPaste = 1,

    [Description("flowlauncher_plugin_clipboardplus_click_action_copy_delete_list")]
    CopyDeleteList = 2,

    [Description("flowlauncher_plugin_clipboardplus_click_action_copy_delete_both")]
    CopyDeleteListDatabase = 3,

    [Description("flowlauncher_plugin_clipboardplus_click_action_copy_paste_delete_list")]
    CopyPasteDeleteList = 4,

    [Description("flowlauncher_plugin_clipboardplus_click_action_copy_paste_delete_both")]
    CopyPasteDeleteListDatabase = 5,
}
