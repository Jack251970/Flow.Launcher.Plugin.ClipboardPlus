// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardHandleWin is a class that handles the clipboard
/// https://learn.microsoft.com/en-us/uwp/api/clipboard
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
internal class ClipboardHandleWin : BaseClipboardHandle, IDisposable
{
    #region Fields

    private static string ClassName => nameof(ClipboardHandleWin);

    private PluginInitContext? _context;

    private bool _ready;

    #endregion

    #region Properties

    /// <summary>
    /// Checks if the handle is ready to monitor the system clipboard.
    /// It is used to provide a final value for use whenever the property
    /// 'ObserveLastEntry' is enabled.
    /// </summary>
    [Browsable(false)]
    internal bool Ready
    {
        get
        {
            if (ClipboardMonitorInstance.ObserveLastEntry)
            {
                _ready = true;
            }
            return _ready;
        }
        set => _ready = value;
    }

    // instant in monitor
    internal ClipboardMonitorWin ClipboardMonitorInstance { get; set; } = null!;

    #endregion

    #region Methods

    #region Initialization

    public void SetContext(PluginInitContext context)
    {
        _context = context;
    }

    #endregion

    #region Clipboard Management

    /// <summary>
    /// Starts monitoring the system clipboard.
    /// </summary>
    public void StartMonitoring()
    {
        Clipboard.ContentChanged += OnClipboardChanged;
        Ready = true;
    }

    /// <summary>
    /// Stops monitoring the system clipboard.
    /// </summary>
    public void StopMonitoring()
    {
        Clipboard.ContentChanged -= OnClipboardChanged;
        _context.LogDebug(ClassName, "Clipboard content changed listener removed.");
        Ready = false;
    }

    /// <summary>
    /// Handles the clipboard data change event.
    /// </summary>
    private async void OnClipboardChanged(object? sender, object _)
    {
        try
        {
            // If clipboard-monitoring is enabled, proceed to listening.
            if ((!Ready) || ClipboardMonitorInstance == null || (!ClipboardMonitorInstance.MonitorClipboard))
            {
                return;
            }

            // If the clipboard is empty, return.
            var dataObj = Clipboard.GetContent();
            if (dataObj is null)
            {
                return;
            }

            // Here no need to handle clipboard action in sta thread
            // Determines whether a file/files have been cut/copied.
            if (ClipboardMonitorInstance.ObservableFormats.Images && IsDataImage(dataObj))
            {
                // Make sure on the application dispatcher.
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        if (await GetImageContentAsync(dataObj) is BitmapImage capturedImage)
                        {
                            ClipboardMonitorInstance.ClipboardImage = capturedImage;

                            if (GetApplicationInfo())
                            {
                                ClipboardMonitorInstance.Invoke(
                                    capturedImage,
                                    DataType.Image,
                                    new SourceApplication(
                                        _executableHandle,
                                        _executableName,
                                        _executableTitle,
                                        _executablePath
                                    )
                                );
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                });
            }
            // Determines whether plain text or rich text has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Texts && IsDataText(dataObj))
            {
                var (plainText, richText, dataType) = await GetTextContentAsync(dataObj);
                ClipboardMonitorInstance.ClipboardText = plainText;
                ClipboardMonitorInstance.ClipboardRtfText = richText;

                if (GetApplicationInfo())
                {
                    ClipboardMonitorInstance.Invoke(
                        dataType == DataType.PlainText ? plainText : richText,
                        dataType,
                        new SourceApplication(
                            _executableHandle,
                            _executableName,
                            _executableTitle,
                            _executablePath
                        )
                    );
                }
            }
            // Determines whether a file has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Files && IsDataFiles(dataObj))
            {
                // If the 'capturedFiles' string array persists as null, then this means
                // that the copied content is of a complex object type since the file-drop
                // format is able to capture more-than-just-file content in the clipboard.
                // Therefore assign the content its rightful type.
                if (await GetFilesContentAsync(dataObj) is string[] capturedFiles)
                {
                    // Clear all existing files before update.
                    ClipboardMonitorInstance.ClipboardFiles.Clear();
                    ClipboardMonitorInstance.ClipboardFiles.AddRange(capturedFiles);
                    ClipboardMonitorInstance.ClipboardFile = capturedFiles[0];

                    if (GetApplicationInfo())
                    {
                        ClipboardMonitorInstance.Invoke(
                            capturedFiles,
                            DataType.Files,
                            new SourceApplication(
                                _executableHandle,
                                _executableName,
                                _executableTitle,
                                _executablePath
                            )
                        );
                    }
                }
                else
                {
                    ClipboardMonitorInstance.ClipboardObject = dataObj;
                    ClipboardMonitorInstance.ClipboardText = await dataObj.GetTextAsync() ?? string.Empty;

                    if (GetApplicationInfo())
                    {
                        ClipboardMonitorInstance.Invoke(
                            dataObj,
                            DataType.Other,
                            new SourceApplication(
                                _executableHandle,
                                _executableName,
                                _executableTitle,
                                _executablePath
                            )
                        );
                    }
                }
            }
            // Determines whether an unknown object has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Others && (!IsDataFiles(dataObj)))
            {
                if (GetApplicationInfo())
                {
                    ClipboardMonitorInstance.Invoke(
                        dataObj,
                        DataType.Other,
                        new SourceApplication(
                            _executableHandle,
                            _executableName,
                            _executableTitle,
                            _executablePath
                        )
                    );
                }
            }
        }
        catch (AccessViolationException)
        {
            // Use-cases such as Remote Desktop usage might throw this exception.
            // Applications with Administrative privileges can however override
            // this exception when run in a production environment.
        }
        catch (COMException e) when (e.HResult == CLIPBOARD_E_CANT_OPEN)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800401D0)
            // OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))
        }
        catch (COMException e) when (e.HResult == CLIPBOARD_E_BAD_DATA)
        {
            // Sometimes data on clipboard is invalid.
            // System.Runtime.InteropServices.COMException (0x800401D3)
            // Bad data in clipboard (0x800401D3 (CLIPBRD_E_BAD_DATA))
        }
        catch (COMException e) when (e.HResult == RPC_SERVER_UNAVAILABLE)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800706BA)
            // RPC server is unavailable (0x800706BA (RPC_E_SERVER_UNAVAILABLE))
        }
        catch (COMException e) when (e.HResult == DV_E_FORMATETC || e.HResult == DV_E_CLIPFORMAT)
        {
            // Sometimes the "FileGroupDescriptorW" format provided by the source app contains a virtual folder item, which cannot be represented as a StorageItem object.
            // System.Runtime.InteropServices.COMException (0x800706BA)
            // Invalid clipboard format (0x8004006A (DV_E_CLIPFORMAT))
        }
        catch (Exception e)
        {
            _context.LogException(ClassName, "Clipboard changed event failed.", e);
        }
    }

    #region Helper Methods

    public static bool IsDataImage(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Bitmap);
    }

    public static bool IsDataText(DataPackageView dataObj)
    {
        return IsDataPlainText(dataObj) || IsDataRichText(dataObj);
    }

    private static bool IsDataPlainText(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Text) ||
            dataObj.Contains("AnsiText") ||
            dataObj.Contains("OEMText") ||
            dataObj.Contains("TEXT");
    }

    private static bool IsDataRichText(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Rtf);
    }

    public static bool IsDataFiles(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.StorageItems);
    }

    public static async Task<BitmapImage?> GetImageContentAsync(DataPackageView dataObj)
    {
        try
        {
            if (await dataObj.GetBitmapAsync() is RandomAccessStreamReference imageReceived)
            {
                // Open the stream reference
                using var imageStream = await imageReceived.OpenReadAsync();

                // Convert to .NET stream and copy to MemoryStream
                using var netStream = imageStream.AsStreamForRead();
                using var memoryStream = new MemoryStream();
                await netStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset position for reading

                try
                {
                    // Create and configure BitmapImage
                    var capturedImage = new BitmapImage();
                    capturedImage.BeginInit();
                    capturedImage.CacheOption = BitmapCacheOption.OnLoad; // Critical for immediate load
                    capturedImage.StreamSource = memoryStream;
                    capturedImage.EndInit();

                    // Enable cross-thread access
                    if (capturedImage.CanFreeze)
                    {
                        capturedImage.Freeze();
                    }

                    return capturedImage;
                }
                catch (NotSupportedException ex) when (ex.InnerException is COMException comEx && (uint)comEx.HResult == 0x88982F50)
                {
                    // System.Runtime.InteropServices.COMException (0x88982F50): The component cannot be found. (0x88982F50)
                }
            }
        }
        catch (Exception)
        {
            // Ignored
        }

        return null;
    }

    public static async Task<(string, string, DataType)> GetTextContentAsync(DataPackageView dataObj)
    {
        var plainText = string.Empty;
        if (IsDataPlainText(dataObj))
        {
            plainText = await GetTextAsync(dataObj) ?? string.Empty;
        }

        var richText = string.Empty;
        if (IsDataRichText(dataObj))
        {
            richText = await dataObj.GetRtfAsync() ?? string.Empty;
        }

        return (plainText, richText, string.IsNullOrEmpty(richText) ? DataType.PlainText : DataType.RichText);
    }

    /// <summary>
    /// Inspired from: https://github.com/WindowSill-app/WindowSill.ClipboardHistory
    /// </summary>
    private static async Task<string> GetTextAsync(DataPackageView dataObj)
    {
        string? text = null;
        if (dataObj.Contains(StandardDataFormats.Text))
        {
            text = await dataObj.GetTextAsync();
        }
        else if (dataObj.Contains("AnsiText"))
        {
            text = await dataObj.GetDataAsync("AnsiText") as string;
        }
        else if (dataObj.Contains("OEMText"))
        {
            text = await dataObj.GetDataAsync("OEMText") as string;
        }
        else if (dataObj.Contains("TEXT"))
        {
            text = await dataObj.GetDataAsync("TEXT") as string;
        }
        return text ?? string.Empty;
    }

    public static async Task<string[]?> GetFilesContentAsync(DataPackageView dataObj)
    {
        // Edited from: FilesystemHelper.cs in https://github.com/files-community/Files
        if (await dataObj.GetStorageItemsAsync() is IReadOnlyList<IStorageItem> source)
        {
            var itemsList = new List<string>();
            foreach (var item in source)
            {
                try
                {
                    itemsList.Add(item.Path);
                }
                catch (Exception ex) when (ex.HResult == DV_E_FORMATETC || ex.HResult == DV_E_CLIPFORMAT)
                {
                    // Not support for files from remote desktop
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return [.. itemsList];
        }

        return null;
    }

    #endregion

    #endregion

    #endregion

    #region IDisposable

    private bool _disposed;

    /// <summary>
    /// Disposes of the clipboard-monitoring resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // <summary>
    /// Disposes all the resources associated with this component.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopMonitoring();
            _disposed = true;
        }
    }

    #endregion
}
