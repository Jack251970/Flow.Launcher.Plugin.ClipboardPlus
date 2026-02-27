using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;

[JsonDerivedType(typeof(ExeAppInfo), typeDiscriminator: "exe")]
[JsonDerivedType(typeof(ShortcutAppInfo), typeDiscriminator: "shortcut")]
[JsonDerivedType(typeof(UwpAppInfo), typeDiscriminator: "uwp")]
[DebuggerDisplay("{DisplayName}")]
public abstract partial class AppInfo : IJsonOnDeserialized, IEquatable<AppInfo>
{
    [JsonPropertyName("default_display_name")]
    public string DefaultDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("override_app_icon_path")]
    public string OverrideAppIconPath { get; set; } = string.Empty;

    // Parameterless constructor for serialization
    protected AppInfo() { }

    /*protected bool _loadIcon = false;

    [JsonIgnore]
    public TaskCompletionNotifier<ImageSource?> AppIcon { get; set; }
        = new(() => Task.FromResult<ImageSource?>(null), runTaskImmediately: false);*/

    public virtual void OnDeserialized()
    {
        /*if (_loadIcon) AppIcon = new TaskCompletionNotifier<ImageSource>(() => IconHelper.GetIconFromFileOrFolderAsync(OverrideAppIconPath), runTaskImmediately: false);*/
    }

    public abstract AppInfo Clone();

    public override bool Equals(object? obj)
    {
        return obj is AppInfo other && Equals(other);
    }

    public bool Equals(AppInfo? other)
    {
        return DefaultDisplayName == other?.DefaultDisplayName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DefaultDisplayName);
    }
}
