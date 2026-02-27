using Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

[SupportedOSPlatform("windows10.0.19041.0")]
public class UwpAppHelper
{
    private static readonly PackageCatalog? packageCatalog;

    private static Task<List<UwpAppInfo>>? uwpAppsListTask;

    static UwpAppHelper()
    {
        // Subscribe to package changes to invalidate cache
        try
        {
            packageCatalog = PackageCatalog.OpenForCurrentUser();
            packageCatalog.PackageInstalling += OnPackageChanged;
            packageCatalog.PackageUninstalling += OnPackageChanged;
            packageCatalog.PackageUpdating += OnPackageChanged;
            packageCatalog.PackageStatusChanged += OnPackageStatusChanged;
        }
        catch (Exception)
        {
            // Error while subscribing to package catalog events
        }
    }

    public static async Task<List<UwpAppInfo>> GetUwpAppsAsync()
    {
        uwpAppsListTask ??= LoadUwpAppsAsync();
        return await uwpAppsListTask;
    }

    private static void OnPackageChanged(PackageCatalog sender, PackageInstallingEventArgs args)
    {
        InvalidateCache();
    }

    private static void OnPackageChanged(PackageCatalog sender, PackageUninstallingEventArgs args)
    {
        InvalidateCache();
    }

    private static void OnPackageChanged(PackageCatalog sender, PackageUpdatingEventArgs args)
    {
        InvalidateCache();
    }

    private static void OnPackageStatusChanged(PackageCatalog sender, PackageStatusChangedEventArgs args)
    {
        InvalidateCache();
    }

    private static void InvalidateCache()
    {
        if (uwpAppsListTask is not null)
        {
            // Package catalog changed, invalidating UWP apps cache
            uwpAppsListTask = LoadUwpAppsAsync();
        }
    }

    private static Task<List<UwpAppInfo>> LoadUwpAppsAsync()
    {
        return Task.Run(() =>
        {
            List<UwpAppInfo> uwpApps = [];

            try
            {
                // We retrieve the installed Microsoft Store apps.
                var packageManager = new PackageManager();
                IEnumerable<Package> packages = packageManager.FindPackagesForUser("");

                foreach (Package package in packages)
                {
                    foreach (AppListEntry appListEntry in package.GetAppListEntries())
                    {
                        var uwpAppInfo = new UwpAppInfo
                        {
                            AppUserModelId = appListEntry.AppUserModelId,
                            PackageFullName = package.Id.FullName,
                            DefaultDisplayName = appListEntry.DisplayInfo.DisplayName,
                            DisplayName = appListEntry.DisplayInfo.DisplayName,
                            Package = package
                        };
                        uwpAppInfo.OnDeserialized();
                        uwpApps.Add(uwpAppInfo);
                    }
                }
            }
            catch (Exception)
            {
                // Error while getting Microsoft Store apps
            }

            return uwpApps;
        });
    }
}
