using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;

public sealed partial class ExeAppInfo : AppInfo, IJsonOnDeserialized, IEquatable<ExeAppInfo>
{
    [JsonPropertyName("exe_file_path")]
    public string ExeFilePath { get; set; } = string.Empty;

    public override string Path => ExeFilePath;

    // Parameterless constructor for XML serialization
    public ExeAppInfo() { }

    public override bool Equals(object? obj)
    {
        return (obj is ExeAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(ExeAppInfo? other)
    {
        return base.Equals(other) && ExeFilePath == other?.ExeFilePath;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ExeFilePath);
    }

    public override void OnDeserialized()
    {
        /*if (string.IsNullOrEmpty(OverrideAppIconPath) && !string.IsNullOrWhiteSpace(ExeFilePath) && File.Exists(ExeFilePath))
        {
            if (_loadIcon) AppIcon = new TaskCompletionNotifier<ImageSource>(() => IconHelper.GetIconFromFileOrFolderAsync(ExeFilePath), runTaskImmediately: false);
        }
        else*/
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new ExeAppInfo
        {
            DefaultDisplayName = DefaultDisplayName,
            DisplayName = DisplayName,
            OverrideAppIconPath = OverrideAppIconPath,
            ExeFilePath = ExeFilePath,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }
}
