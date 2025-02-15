﻿// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
/// https://docs.microsoft.com/windows/release-health/release-information
/// </summary>
public class WindowsClipboardHelper : IDisposable
{
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

    #endregion

    #region Constructors

    public ISettings Settings { get; set; } = null!;
    public SqliteDatabase Database { get; set; } = null!;

    public WindowsClipboardHelper()
    {
        Clipboard_HistoryChanged(this, null!);
        RegisterHistoryChangedEvent();
    }

    #endregion

    #region Events

    public event EventHandler<ClipboardData>? OnHistoryItemAdded;
    public event EventHandler<List<string>>? OnHistoryItemRemoved;
    public event EventHandler<ClipboardData>? OnHistoryItemPinUpdated;

    private readonly SemaphoreSlim _historyItemLock = new(1, 1);

    private readonly List<string> _clipboardHistoryItemsIds = new();
    private readonly List<ClipboardHistoryItem> _clipboardHistoryItems = new();

    private bool RegisterHistoryChangedEvent()
    {
        if (IsClipboardHistorySupported())
        {
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged += Clipboard_HistoryChanged;
            return true;
        }

        return false;
    }

    private bool UnregisterHistoryChangedEvent()
    {
        if (IsClipboardHistorySupported())
        {
            Windows.ApplicationModel.DataTransfer.Clipboard.HistoryChanged -= Clipboard_HistoryChanged;
            return true;
        }

        return false;
    }

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
                                OnHistoryItemAdded?.Invoke(this, GetClipboardData(newItem));
                            }
                        }
                        else if (_clipboardHistoryItems.Count > items.Count)  // remove 1 item
                        {
                            if (OnHistoryItemRemoved != null)
                            {
                                var newClipboardHistoryItemsIds = items.Select(x => x.Id).ToList();
                                var removedItems = _clipboardHistoryItems
                                    .Where(x => !newClipboardHistoryItemsIds.Contains(x.Id))
                                    .Select(x => x.Id)
                                    .ToList();
                                OnHistoryItemRemoved.Invoke(this, removedItems);
                            }
                        }
                        else
                        {
                            if (OnHistoryItemPinUpdated != null)
                            {
                                // No idea how to get the updated item
                                OnHistoryItemPinUpdated.Invoke(this, ClipboardData.NULL);
                            }
                        }
                    }

                    // refresh the list
                    _clipboardHistoryItems.Clear();
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

    #endregion

    public async Task<List<ClipboardData>?> GetHistoryItemsAsync()
    {
        if (IsClipboardHistorySupported())
        {
            await _historyItemLock.WaitAsync();
            try
            {
                var clipboardDataItems = new List<ClipboardData>();
                foreach (var item in _clipboardHistoryItems)
                {
                    clipboardDataItems.Add(GetClipboardData(item));
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

    private ClipboardData GetClipboardData(ClipboardHistoryItem item)
    {
#if DEBUG
        if (Settings == null || Database == null)
        {
            return new ClipboardData(item.Content, DataType.Other, false)
            {
                // item.Id with numbers and capital letters, while StringUtils.GetGuid() with numbers and lowercase letters
                HashId = item.Id,
                SenderApp = string.Empty,
                InitScore = 0,
                CachedImagePath = string.Empty,
                CreateTime = item.Timestamp.DateTime,
                Pinned = false,
                Saved = false,
                PlainText = string.Empty,
                EncryptKeyMd5 = StringUtils.EncryptKeyMd5
            };
        }
#endif
        return new ClipboardData(item.Content, DataType.Other, Settings.EncryptData)
        {
            // item.Id with numbers and capital letters, while StringUtils.GetGuid() with numbers and lowercase letters
            HashId = item.Id,
            SenderApp = string.Empty,
            InitScore = Database.CurrentScore,
            CachedImagePath = string.Empty,
            CreateTime = item.Timestamp.DateTime,
            Pinned = false,
            Saved = false,
            PlainText = string.Empty,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5
        };
    }

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
            OnHistoryItemAdded = null;
            OnHistoryItemRemoved = null;
            OnHistoryItemPinUpdated = null;
            UnregisterHistoryChangedEvent();
            _clipboardHistoryItems.Clear();
            _historyItemLock.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
