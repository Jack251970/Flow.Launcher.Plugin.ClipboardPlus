using Windows.ApplicationModel.DataTransfer;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

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
            return Clipboard.IsHistoryEnabled();
        }

        return false;
    }

    public static bool ClearHistory()
    {
        if (IsClipboardHistorySupported())
        {
            return Clipboard.ClearHistory();
        }

        return false;
    }

    public static async Task<List<ClipboardData>> GetHistoryItemsAsync(ISettings settings, SqliteDatabase database)
    {
        var clipboardDataItems = new List<ClipboardData>();

        if (IsClipboardHistorySupported())
        {
            var historyItems = await Clipboard.GetHistoryItemsAsync();
            if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                // TODO
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
                        UnicodeText = string.Empty,
                        EncryptKeyMd5 = StringUtils.EncryptKeyMd5
                    });
                }*/
            }
        }

        return clipboardDataItems;
    }

#pragma warning restore CA1416 // Validate platform compatibility

}
