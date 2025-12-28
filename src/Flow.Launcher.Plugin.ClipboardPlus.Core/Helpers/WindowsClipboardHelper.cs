// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// https://learn.microsoft.com/en-us/uwp/api/clipboard
/// https://docs.microsoft.com/windows/release-health/release-information
/// </summary>
public class WindowsClipboardHelper : IDisposable
{
    private static string ClassName => nameof(WindowsClipboardHelper);

    #region Helper Operations

    public static bool IsClipboardHistorySupported()
    {
        return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
    }

    public static bool IsHistoryEnabled()
    {
        if (IsClipboardHistorySupported())
        {
            return Clipboard.IsHistoryEnabled();
        }

        return false;
    }

    public static async Task<int> ClearUnpinnnedRecordsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            var historyItems = await Clipboard.GetHistoryItemsAsync();
            if (Clipboard.ClearHistory() && historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                var historyItemsAfter = await Clipboard.GetHistoryItemsAsync();
                if (historyItemsAfter.Status == ClipboardHistoryItemsResultStatus.Success)
                {
                    return historyItems.Items.Count - historyItemsAfter.Items.Count;
                }
            }
        }

        return -1;
    }

    public static async Task<int> ClearAllRecordsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            var historyItems = await Clipboard.GetHistoryItemsAsync();
            if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                foreach (var item in historyItems.Items)
                {
                    Clipboard.DeleteItemFromHistory(item);
                }
                return historyItems.Items.Count;
            }
        }

        return -1;
    }

    public static bool DeleteItemFromHistory(ClipboardData clipboardData)
    {
        if (IsClipboardHistorySupported() && clipboardData.ClipboardHistoryItem is ClipboardHistoryItem item)
        {
            return Clipboard.DeleteItemFromHistory(item);
        }

        return false;
    }

    #endregion

    #region Pinned Items Helper

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Represents the metadata.json structure for pinned clipboard items
    /// </summary>
    private class PinnedClipboardMetadata
    {
        public Dictionary<string, PinnedItemInfo>? Items { get; set; }
    }

    private class PinnedItemInfo
    {
        public string? Timestamp { get; set; }
        public string? Source { get; set; }
    }

    /// <summary>
    /// Gets the set of pinned clipboard item IDs from Windows clipboard pinned folder
    /// </summary>
    /// <returns>HashSet of pinned item IDs (in lowercase)</returns>
    private HashSet<string> GetPinnedClipboardItemIds()
    {
        var pinnedIds = new HashSet<string>();

        try
        {
            // https://superuser.com/questions/1778831/where-is-the-file-that-contains-pinned-items-in-windows-10-clipboard
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var clipboardPinnedPath = Path.Combine(localAppData, "Microsoft", "Windows", "Clipboard", "Pinned");

            if (!Directory.Exists(clipboardPinnedPath))
            {
                return pinnedIds;
            }

            // Enumerate all GUID directories under Pinned
            var guidDirs = Directory.GetDirectories(clipboardPinnedPath);
            foreach (var guidDir in guidDirs)
            {
                var metadataPath = Path.Combine(guidDir, "metadata.json");
                if (File.Exists(metadataPath))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(metadataPath);
                        var metadata = JsonSerializer.Deserialize<PinnedClipboardMetadata>(jsonContent, _jsonOptions);
                        
                        if (metadata?.Items != null)
                        {
                            foreach (var itemId in metadata.Items.Keys)
                            {
                                // Remove curly braces from GUID format (e.g., "{GUID}" -> "GUID")
                                // and convert to lowercase to match ClipboardHistoryItem.Id format
                                var normalizedId = itemId.Trim('{', '}').ToLower();
                                pinnedIds.Add(normalizedId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other metadata files
                        _context.LogException(ClassName, $"Failed to read metadata file: {metadataPath}", ex, "GetPinnedClipboardItemIds");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error accessing the pinned folder
            _context.LogException(ClassName, "Failed to access Windows clipboard pinned folder", ex, "GetPinnedClipboardItemIds");
        }

        return pinnedIds;
    }

    #endregion

    #region Initialization

    private IClipboardPlus _clipboardPlus = null!;
    private PluginInitContext? _context => _clipboardPlus.Context;

    public void SetClipboardPlus(IClipboardPlus clipboardPlus)
    {
        _clipboardPlus = clipboardPlus;
    }

    #endregion

    #region Enable / Disable

    public void EnableClipboardHistory()
    {
        if (IsClipboardHistorySupported())
        {
            _previousPinnedItemIds = GetPinnedClipboardItemIds();
            Clipboard_HistoryChanged(this, null!);
            Clipboard.HistoryChanged += Clipboard_HistoryChanged;
            Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
        }
    }

    public void DisableClipboardHistory()
    {
        if (IsClipboardHistorySupported())
        {
            _clipboardHistoryItems.Clear();
            _clipboardHistoryItemsIds.Clear();
            _previousPinnedItemIds.Clear();
            Clipboard.HistoryChanged -= Clipboard_HistoryChanged;
            Clipboard.HistoryEnabledChanged -= Clipboard_HistoryEnabledChanged;
        }
    }

    #endregion

    #region Events

    public event EventHandler<ClipboardData>? OnHistoryItemAdded;
    public event EventHandler<string[]>? OnHistoryItemRemoved;
    public event EventHandler<ClipboardData>? OnHistoryItemPinUpdated;

    public event EventHandler<bool>? OnHistoryEnabledChanged;

    private readonly SemaphoreSlim _historyItemLock = new(1, 1);

    private readonly List<string> _clipboardHistoryItemsIds = [];
    private readonly List<ClipboardHistoryItem> _clipboardHistoryItems = [];
    private HashSet<string> _previousPinnedItemIds = [];

    private void Clipboard_HistoryChanged(object? sender, ClipboardHistoryChangedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await _historyItemLock.WaitAsync();
            try
            {
                var historyItems = await Clipboard.GetHistoryItemsAsync();
                if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
                {
                    var items = historyItems.Items;

                    // invoke the event
                    if (e != null)
                    {
                        if (_clipboardHistoryItems.Count < items.Count)  // Item added
                        {
                            await AddItemAsync(items);
                        }
                        else if (_clipboardHistoryItems.Count > items.Count)  // Item removed
                        {
                            RemoveItem(items);
                        }
                        else // Windows clipboard history is full, or item pin updated
                        {
                            if (!await UpdatedPinnedItemsAsync(items))
                            {
                                RemoveItem(items);
                                await AddItemAsync(items);
                            }
                        }
                    }

                    // refresh the list
                    _clipboardHistoryItems.Clear();
                    _clipboardHistoryItemsIds.Clear();
                    foreach (var item in items)
                    {
                        _clipboardHistoryItems.Add(item);
                        _clipboardHistoryItemsIds.Add(item.Id);
                    }
                }
            }
            finally
            {
                _historyItemLock.Release();
            }

            void RemoveItem(IReadOnlyList<ClipboardHistoryItem> items)
            {
                if (OnHistoryItemRemoved == null) return;
                var newClipboardHistoryItemsIds = items.Select(x => x.Id).ToList();
                var removedItems = _clipboardHistoryItems
                    .Where(x => !newClipboardHistoryItemsIds.Contains(x.Id))
                    .Select(x => GetHashId(x.Id))
                    .ToArray();
                if (removedItems.Length == 0) return;
                OnHistoryItemRemoved.Invoke(this, removedItems);
                _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: Removed items: {string.Join(", ", removedItems)}");
            }

            async Task AddItemAsync(IReadOnlyList<ClipboardHistoryItem> items)
            {
                if (OnHistoryItemAdded == null) return;
                var newItem = items.First(x => !_clipboardHistoryItemsIds.Contains(x.Id));
                if (newItem == null) return;
                OnHistoryItemAdded.Invoke(this, await GetClipboardData(newItem));
                _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: Added item: {newItem.Id}");
            }

            async Task<bool> UpdatedPinnedItemsAsync(IReadOnlyList<ClipboardHistoryItem> items)
            {
                if (OnHistoryItemPinUpdated == null) return false;
                
                // Get current pinned item IDs from Windows clipboard metadata
                var currentPinnedIds = GetPinnedClipboardItemIds();
                
                // Find items whose pin status changed
                var itemsWithChangedPinStatus = new List<ClipboardHistoryItem>();
                
                foreach (var item in items)
                {
                    var itemId = GetHashId(item.Id);
                    var isPinnedNow = currentPinnedIds.Contains(itemId);
                    var wasPinnedBefore = _previousPinnedItemIds.Contains(itemId);
                    
                    // Check if pin status changed
                    if (isPinnedNow != wasPinnedBefore)
                    {
                        itemsWithChangedPinStatus.Add(item);
                    }
                }
                
                // Update the previous pinned state
                _previousPinnedItemIds = currentPinnedIds;
                
                // If we found items with changed pin status, invoke the event
                if (itemsWithChangedPinStatus.Count > 0)
                {
                    foreach (var item in itemsWithChangedPinStatus)
                    {
                        var clipboardData = await GetClipboardData(item);
                        if (!clipboardData.IsNull())
                        {
                            OnHistoryItemPinUpdated.Invoke(this, clipboardData);
                            _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: Pin status updated for item: {item.Id}");
                        }
                    }
                    return true;
                }
                
                return false;
            }
        });
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        if (IsHistoryEnabled())
        {
            _previousPinnedItemIds = GetPinnedClipboardItemIds();
            Clipboard_HistoryChanged(this, null!);
        }
        else
        {
            _clipboardHistoryItems.Clear();
            _clipboardHistoryItemsIds.Clear();
            _previousPinnedItemIds.Clear();
        }
        OnHistoryEnabledChanged?.Invoke(this, IsHistoryEnabled());
    }

    #endregion

    #region History Items

    public async Task<List<ClipboardData>?> GetHistoryItemsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            await _historyItemLock.WaitAsync();
            try
            {
                // filter and sort the items (later item last)
                var laterSortedItems = _clipboardHistoryItems
                    .OrderBy(x => x.Timestamp.DateTime)
                    .ToList();

                // get the clipboard data
                var clipboardDataItems = new List<ClipboardData>();
                foreach (var item in laterSortedItems)
                {
                    var clipboardData = await GetClipboardData(item);
                    if (!clipboardData.IsNull())
                    {
                        clipboardDataItems.Add(clipboardData);
                        _clipboardPlus.ScoreHelper.Add();
                    }
                }
                return clipboardDataItems;
            }
            finally
            {
                _historyItemLock.Release();
            }
        }

        return null;
    }

    public async Task<List<ClipboardData>?> GetLaterHistoryItemsAsync(DateTime dateTime)
    {
        if (IsClipboardHistorySupported())
        {
            await _historyItemLock.WaitAsync();
            try
            {
                // filter and sort the items (later item last)
                var laterSortedItems = _clipboardHistoryItems
                    .Where(x => x.Timestamp.DateTime > dateTime)
                    .OrderBy(x => x.Timestamp.DateTime)
                    .ToList();

                // get the clipboard data
                var clipboardDataItems = new List<ClipboardData>();
                foreach (var item in laterSortedItems)
                {
                    var clipboardData = await GetClipboardData(item);
                    if (!clipboardData.IsNull())
                    {
                        clipboardDataItems.Add(clipboardData);
                        _clipboardPlus.ScoreHelper.Add();
                    }
                }
                return clipboardDataItems;
            }
            finally
            {
                _historyItemLock.Release();
            }
        }

        return null;
    }

    private async Task<ClipboardData> GetClipboardData(ClipboardHistoryItem item)
    {
        // If the clipboard is empty, return.
        var dataObj = item.Content;
        if (dataObj == null)
        {
            return ClipboardData.NULL;
        }

        // Get hash id & create time
        var hashId = GetHashId(item.Id);
        var createTime = item.Timestamp.DateTime;

        // Determines whether a file/files have been cut/copied.
        if (_clipboardPlus.ObservableDataFormats.Images && ClipboardHandleWin.IsDataImage(dataObj))
        {
            // Make sure on the application dispatcher.
            var clipboardData = await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (await ClipboardHandleWin.GetImageContentAsync(dataObj) is BitmapImage capturedImage)
                    {
                        var clipboardData = _clipboardPlus.GetClipboardDataItem(
                            capturedImage,
                            DataType.Image,
                            hashId,
                            createTime,
                            SourceApplication.NULL,
                            string.Empty,
                            string.Empty
                        );
                        if (!clipboardData.IsNull())
                        {
                            clipboardData.ClipboardHistoryItem = item;
                        }
                        return clipboardData;
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }

                return ClipboardData.NULL;
            });
            return await clipboardData;
        }
        // Determines whether plain text or rich text has been cut/copied.
        else if (_clipboardPlus.ObservableDataFormats.Texts && ClipboardHandleWin.IsDataText(dataObj))
        {
            var (plainText, richText, dataType) = await ClipboardHandleWin.GetTextContentAsync(dataObj);
            var clipboardData = _clipboardPlus.GetClipboardDataItem(
                dataType == DataType.PlainText ? plainText : richText,
                dataType,
                hashId,
                createTime,
                SourceApplication.NULL,
                plainText,
                richText
            );
            if (!clipboardData.IsNull())
            {
                clipboardData.ClipboardHistoryItem = item;
            }
            return clipboardData;
        }
        // Determines whether a file has been cut/copied.
        else if (_clipboardPlus.ObservableDataFormats.Files && ClipboardHandleWin.IsDataFiles(dataObj))
        {
            // If the 'capturedFiles' string array persists as null, then this means
            // that the copied content is of a complex object type since the file-drop
            // format is able to capture more-than-just-file content in the clipboard.
            // Therefore assign the content its rightful type.
            if (await ClipboardHandleWin.GetFilesContentAsync(dataObj) is string[] capturedFiles)
            {
                var clipboardData = _clipboardPlus.GetClipboardDataItem(
                    capturedFiles,
                    DataType.Files,
                    hashId,
                    createTime,
                    SourceApplication.NULL,
                    string.Empty,
                    string.Empty
                );
                if (!clipboardData.IsNull())
                {
                    clipboardData.ClipboardHistoryItem = item;
                }
                return clipboardData;
            }
            else
            {
                var clipboardData = _clipboardPlus.GetClipboardDataItem(
                    dataObj,
                    DataType.Other,
                    hashId,
                    createTime,
                    SourceApplication.NULL,
                    string.Empty,
                    string.Empty
                );
                if (!clipboardData.IsNull())
                {
                    clipboardData.ClipboardHistoryItem = item;
                }
                return clipboardData;
            }
        }
        // Determines whether an unknown object has been cut/copied.
        else if (_clipboardPlus.ObservableDataFormats.Others && (!ClipboardHandleWin.IsDataFiles(dataObj)))
        {
            var clipboardData = _clipboardPlus.GetClipboardDataItem(
                dataObj,
                DataType.Other,
                hashId,
                createTime,
                SourceApplication.NULL,
                string.Empty,
                string.Empty
            );
            if (!clipboardData.IsNull())
            {
                clipboardData.ClipboardHistoryItem = item;
            }
            return clipboardData;
        }

        return ClipboardData.NULL;
    }

    // item.Id with numbers and capital letters, while StringUtils.GetGuid() with numbers and lowercase letters
    // Therefore, we need to convert the item.Id to lowercase.
    private static string GetHashId(string itemId)
    {
        return itemId.ToLower();
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisableClipboardHistory();
            OnHistoryItemAdded = null;
            OnHistoryItemRemoved = null;
            OnHistoryItemPinUpdated = null;
            _historyItemLock.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
