using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;

public sealed partial class ShortcutAppInfo : AppInfo, IEquatable<ShortcutAppInfo>
{
    [JsonPropertyName("shortcut_file_path")]
    public string ShortcutFilePath { get; set; } = string.Empty;

    [JsonPropertyName("icon_path")]
    public string IconPath { get; set; } = string.Empty;

    [JsonPropertyName("target_path")]
    public string TargetPath { get; set; } = string.Empty;

    // Parameterless constructor for XML serialization
    public ShortcutAppInfo() { }

    public override bool Equals(object? obj)
    {
        return (obj is ShortcutAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(ShortcutAppInfo? other)
    {
        return base.Equals(other) && ShortcutFilePath == other?.ShortcutFilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ShortcutFilePath);
    }

    public override void OnDeserialized()
    {
        /*string iconPath = !string.IsNullOrEmpty(IconPath)
            ? IconPath
            : TargetPath;
        if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            if (_loadIcon) AppIcon = new TaskCompletionNotifier<ImageSource>(() => IconHelper.GetIconFromFileOrFolderAsync(iconPath), runTaskImmediately: false);
        }
        else*/
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new ShortcutAppInfo
        {
            DefaultDisplayName = DefaultDisplayName,
            DisplayName = DisplayName,
            OverrideAppIconPath = OverrideAppIconPath,
            ShortcutFilePath = ShortcutFilePath,
            IconPath = IconPath,
            TargetPath = TargetPath,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }
}
