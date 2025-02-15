// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

/// <summary>
/// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
/// https://docs.microsoft.com/windows/release-health/release-information
/// </summary>
public class WindowsClipboardHelper
{
    public static bool IsClipboardHistorySupported()
    {
        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
    }

#pragma warning disable CA1416 // Validate platform compatibility

    public static bool IsHistoryEnabled()
    {
        if (IsClipboardHistorySupported())
        {
            return Windows.ApplicationModel.DataTransfer.Clipboard.IsHistoryEnabled();
        }

        return false;
    }

    public static bool ClearUnpinnnedRecords()
    {
        if (IsClipboardHistorySupported())
        {
            return Windows.ApplicationModel.DataTransfer.Clipboard.ClearHistory();
        }

        return false;
    }

    public static async Task<bool> ClearAllRecordsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            var historyItems = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
            if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                Windows.ApplicationModel.DataTransfer.Clipboard.ClearHistory();
                foreach (var item in historyItems.Items)
                {
                    Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(item);
                }
                return true;
            }
        }

        return false;
    }

    public static async Task<List<ClipboardData>> GetHistoryItemsAsync(ISettings settings, SqliteDatabase database)
    {
        var clipboardDataItems = new List<ClipboardData>();

        if (IsClipboardHistorySupported())
        {
            var historyItems = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
            if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                /*foreach (var item in historyItems.Items)
                {
                    clipboardDataItems.Add(new ClipboardData(item.Content, dataType, settings.EncryptData)
                    {
                        HashId = StringUtils.GetGuid(),
                        SenderApp = e.SourceApplication.Name,
                        InitScore = database.CurrentScore,
                        CachedImagePath = string.Empty,
                        CreateTime = item.Timestamp.DateTime,
                        Pinned = false,
                        Saved = false,
                        PlainText = string.Empty,
                        EncryptKeyMd5 = StringUtils.EncryptKeyMd5
                    });
                }*/
            }
        }

        return clipboardDataItems;
    }

#pragma warning restore CA1416 // Validate platform compatibility

}
