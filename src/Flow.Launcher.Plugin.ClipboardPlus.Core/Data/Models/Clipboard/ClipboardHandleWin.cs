// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardHandleWin is a class that handles the clipboard
/// https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
internal class ClipboardHandleWin : IDisposable
{
    #region Fields

    private static string ClassName => typeof(ClipboardHandleWin).Name;

    private PluginInitContext? _context;

    private HWND _handle = HWND.Null;

    private bool _ready;

    private nint _executableHandle = 0;
    private string _executableName = string.Empty;
    private string _executablePath = string.Empty;
    private string _executableTitle = string.Empty;

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

    #region Constructor

    public ClipboardHandleWin()
    {

    }

    public void SetContext(PluginInitContext context)
    {
        _context = context;
    }

    #endregion

    #region Methods

    #region Clipboard Management

    /// <summary>
    /// Starts monitoring the system clipboard.
    /// </summary>
    public void StartMonitoring()
    {
        Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += OnClipboardChanged;

        Ready = true;
    }

    /// <summary>
    /// Stops monitoring the system clipboard.
    /// </summary>
    public void StopMonitoring()
    {
        Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged -= OnClipboardChanged;
    }

    /// <summary>
    /// Handles the clipboard data change event.
    /// </summary>
    private async void OnClipboardChanged(object? sender, object e)
    {
        try
        {
            // If clipboard-monitoring is enabled, proceed to listening.
            if (!Ready || !ClipboardMonitorInstance.MonitorClipboard)
            {
                return;
            }

            // If the clipboard is empty, return.
            var dataObj = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataObj is null)
            {
                return;
            }

            // Here no need to handle clipboard action in sta thread
            // Determines whether a file/files have been cut/copied.
            if (ClipboardMonitorInstance.ObservableFormats.Images && IsDataImage(dataObj))
            {
                // Make sure on the application dispatcher.
                System.Windows.Application.Current.Dispatcher.Invoke((Delegate)(async () =>
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
                }));
            }
            // Determines whether unicode text or rich text has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Texts && IsDataText(dataObj))
            {
                var plainText = string.Empty;
                if (IsDataPlainText(dataObj))
                {
                    plainText = await dataObj.GetTextAsync() ?? string.Empty;
                }
                ClipboardMonitorInstance.ClipboardText = plainText;

                var richText = string.Empty;
                if (IsDataRichText(dataObj))
                {
                    richText = await dataObj.GetRtfAsync() ?? string.Empty;
                }
                ClipboardMonitorInstance.ClipboardRtfText = richText;

                var isPlainText = richText == string.Empty;
                ClipboardMonitorInstance.Invoke(
                    isPlainText ? plainText : richText,
                    isPlainText ? DataType.PlainText : DataType.RichText,
                    new SourceApplication(
                        _executableHandle,
                        _executableName,
                        _executableTitle,
                        _executablePath
                    )
                );
            }
            // Determines whether a file has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Files && IsDataFiles(dataObj))
            {
                // Edited from: FilesystemHelper.cs in https://github.com/files-community/Files
                if (await dataObj.GetStorageItemsAsync() is not IReadOnlyList<IStorageItem> source)
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
                else
                {
                    // Clear all existing files before update.
                    var itemsList = new List<string>();
                    foreach (var item in source)
                    {
                        try
                        {
                            itemsList.Add(item.Path);
                        }
                        catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
                        {
                            // Not support for files from remote desktop
                        }
                        catch (Exception ex)
                        {
                            _context?.API.LogException(ClassName, ex.Message, ex);
                        }
                    }
                    var capturedFiles = itemsList.ToArray();

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
        catch (NullReferenceException) { }
        catch (COMException)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800401D0)
            // OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))
        }
    }

    private static bool IsDataImage(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Bitmap);
    }

    private static bool IsDataText(DataPackageView dataObj)
    {
        return IsDataPlainText(dataObj) || IsDataRichText(dataObj);
    }

    private static bool IsDataPlainText(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Text);
    }

    private static bool IsDataRichText(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.Rtf);
    }

    private static bool IsDataFiles(DataPackageView dataObj)
    {
        return dataObj.Contains(StandardDataFormats.StorageItems);
    }

    #endregion

    #region Souce App Management

    #region Helper Methods

    private unsafe bool GetApplicationInfo()
    {
        _executableHandle = 0;
        _executableName = string.Empty;
        _executableTitle = string.Empty;
        _executablePath = string.Empty;

        try
        {
            var hwnd = PInvoke.GetForegroundWindow();
            _executableHandle = hwnd.Value;

            uint processId = 0;
            _ = PInvoke.GetWindowThreadProcessId(hwnd, &processId);
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;
            if (process.MainModule is ProcessModule processModule)
            {
                _executablePath = processModule.FileName;
                _executableName = _executablePath[(_executablePath.LastIndexOf(@"\", StringComparison.Ordinal) + 1)..];
            }

            const int capacity = 256;
            fixed (char* content = new char[capacity])
            {
                _ = PInvoke.GetWindowText(hwnd, content, capacity);
                _executableTitle = new(content);
            }

            return true;
        }
        catch (Exception)
        {
            // ignored
            return true;
        }
    }

    private static async Task RetryActionAsync(Func<bool> action, int retryInterval = 100, int maxAttemptCount = 3)
    {
        for (int i = 0; i < maxAttemptCount; i++)
        {
            try
            {
                if (action())
                {
                    break;
                }
            }
            catch (Exception)
            {
                if (i == maxAttemptCount - 1)
                {
                    return;
                }
                await Task.Delay(retryInterval);
            }
        }
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
