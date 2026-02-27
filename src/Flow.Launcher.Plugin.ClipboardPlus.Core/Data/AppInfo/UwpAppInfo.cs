using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Management.Deployment;
using Windows.Storage.Streams;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;

[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class UwpAppInfo : AppInfo, IEquatable<UwpAppInfo>
{
    [JsonPropertyName("app_user_model_id")]
    public string AppUserModelId { get; set; } = string.Empty;

    [JsonPropertyName("package_family_name")]
    public string PackageFullName { get; set; } = string.Empty;

    // Parameterless constructor for XML serialization
    public UwpAppInfo() { }

    [JsonIgnore]
    internal Package? Package { get; set; }

    public override bool Equals(object? obj)
    {
        return (obj is UwpAppInfo other && Equals(other)) && base.Equals(obj);
    }

    public bool Equals(UwpAppInfo? other)
    {
        return base.Equals(other) && PackageFullName == other?.PackageFullName && AppUserModelId == other?.AppUserModelId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), PackageFullName, AppUserModelId);
    }

    public override void OnDeserialized()
    {
        if (Package is null)
        {
            var packageManager = new PackageManager();
            Package
                = packageManager.FindPackageForUser(
                    userSecurityId: null,
                    packageFullName: PackageFullName);
            if (Package is null)
            {
                IEnumerable<Package> packages = packageManager.FindPackagesForUser("");
                foreach (Package package in packages)
                {
                    foreach (AppListEntry appListEntry in package.GetAppListEntries())
                    {
                        if (appListEntry.AppUserModelId == AppUserModelId)
                        {
                            Package = package;
                            break;
                        }
                    }

                    if (Package is not null)
                    {
                        break;
                    }
                }
            }
        }

        /*if (Package is not null && string.IsNullOrEmpty(OverrideAppIconPath))
        {
            AppIcon = new TaskCompletionNotifier<ImageSource>(GetUwpAppIconAsync, runTaskImmediately: false);
        }
        else*/
        {
            base.OnDeserialized();
        }
    }

    public override AppInfo Clone()
    {
        var newAppInfo = new UwpAppInfo
        {
            DefaultDisplayName = DefaultDisplayName,
            DisplayName = DisplayName,
            OverrideAppIconPath = OverrideAppIconPath,
            AppUserModelId = AppUserModelId,
            PackageFullName = PackageFullName,
            Package = Package,
        };
        newAppInfo.OnDeserialized();
        return newAppInfo;
    }

    /// <summary>
    /// Retrieve the icon of a UWP app
    /// </summary>
    /// <param name="package">The UWP package</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /*private async Task<ImageSource> GetUwpAppIconAsync()
    {
        if (!_loadIcon) return null;

        try
        {
            if (Package is null)
            {
                return null;
            }

            RandomAccessStreamReference appIcon
                = Package.GetLogoAsRandomAccessStreamReference(
                    new Size(
                        IconHelper.DefaultIconSize * 2,
                        IconHelper.DefaultIconSize * 2));

            if (appIcon is null)
            {
                return null;
            }

            using IRandomAccessStreamWithContentType appIconStream = await appIcon.OpenReadAsync();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(appIconStream);

            using SoftwareBitmap softwareBitmap
                = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied);

            int width = softwareBitmap.PixelWidth;
            int height = softwareBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            softwareBitmap.CopyToBuffer(pixels.AsBuffer());

            System.Windows.Media.Imaging.BitmapSource bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);

            if (bitmapSource.CanFreeze)
            {
                bitmapSource.Freeze();
            }

            return bitmapSource;
        }
        catch (Exception)
        {
            // Failed to extract UWP app icon for the specified package.
            return null;
        }
    }*/
}
