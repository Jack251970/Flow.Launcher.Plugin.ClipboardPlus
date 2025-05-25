// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
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
            return Windows.ApplicationModel.DataTransfer.Clipboard.IsHistoryEnabled();
        }

        return false;
    }

    public static async Task<int> ClearUnpinnnedRecordsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            var historyItems = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
            if (Windows.ApplicationModel.DataTransfer.Clipboard.ClearHistory() && historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                var historyItemsAfter = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
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
            var historyItems = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
            if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
            {
                foreach (var item in historyItems.Items)
                {
                    Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(item);
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
            return Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(item);
        }

        return false;
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
            Clipboard_HistoryChanged(this, null!);
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged += Clipboard_HistoryChanged;
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
        }
    }

    public void DisableClipboardHistory()
    {
        if (IsClipboardHistorySupported())
        {
            _clipboardHistoryItems.Clear();
            _clipboardHistoryItemsIds.Clear();
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged -= Clipboard_HistoryChanged;
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryEnabledChanged -= Clipboard_HistoryEnabledChanged;
        }
    }

    #endregion

    #region Events

    public event EventHandler<ClipboardData>? OnHistoryItemAdded;
    public event EventHandler<string[]>? OnHistoryItemRemoved;
    public event EventHandler<ClipboardData>? OnHistoryItemPinUpdated;

    public event EventHandler<bool>? OnHistoryEnabledChanged;

    private readonly SemaphoreSlim _historyItemLock = new(1, 1);

    private readonly List<string> _clipboardHistoryItemsIds = new();
    private readonly List<ClipboardHistoryItem> _clipboardHistoryItems = new();

    private void Clipboard_HistoryChanged(object? sender, ClipboardHistoryChangedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await _historyItemLock.WaitAsync();
            try
            {
                var historyItems = await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
                if (historyItems.Status == ClipboardHistoryItemsResultStatus.Success)
                {
                    var items = historyItems.Items;

                    // invoke the event
                    if (e != null)
                    {
                        if (_clipboardHistoryItems.Count < items.Count)  // add 1 item
                        {
                            if (OnHistoryItemAdded != null)
                            {
                                var newItem = items.First(x => !_clipboardHistoryItemsIds.Contains(x.Id));
                                OnHistoryItemAdded?.Invoke(this, await GetClipboardData(newItem));
                                _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: Added item: {newItem.Id}");
                            }
                        }
                        else if (_clipboardHistoryItems.Count > items.Count)  // remove 1 item
                        {
                            if (OnHistoryItemRemoved != null)
                            {
                                var newClipboardHistoryItemsIds = items.Select(x => x.Id).ToList();
                                var removedItems = _clipboardHistoryItems
                                    .Where(x => !newClipboardHistoryItemsIds.Contains(x.Id))
                                    .Select(x => GetHashId(x.Id))
                                    .ToArray();
                                OnHistoryItemRemoved.Invoke(this, removedItems);
                                _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: Removed items: {string.Join(", ", removedItems)}");
                            }
                        }
                        else
                        {
                            if (OnHistoryItemPinUpdated != null)
                            {
                                // No idea how to get the updated item
                                OnHistoryItemPinUpdated.Invoke(this, ClipboardData.NULL);
                                _context.LogDebug(ClassName, $"Clipboard_HistoryChanged: No idea how to get the updated item.");
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
        });
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        if (IsHistoryEnabled())
        {
            Clipboard_HistoryChanged(this, null!);
        }
        else
        {
            _clipboardHistoryItems.Clear();
            _clipboardHistoryItemsIds.Clear();
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
                catch (COMException)
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
