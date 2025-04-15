// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardHandleW is a class that handles the clipboard
/// </summary>
[SupportedOSPlatform("windows6.0.6000")]
internal class ClipboardHandleW : BaseClipboardHandle, IDisposable
{
    #region Fields

    private static string ClassName => nameof(ClipboardHandleW);

    private PluginInitContext? _context;

    private bool _ready;

    private HWND _handle = HWND.Null;

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
    internal ClipboardMonitorW ClipboardMonitorInstance { get; set; } = null!;

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
        if (System.Windows.Application.Current.MainWindow.IsLoaded)
        {
            MainWindow_Loaded(null, new RoutedEventArgs());
        }
        else
        {
            System.Windows.Application.Current.MainWindow.Loaded += MainWindow_Loaded;
        }
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Get the handle of the main window.
        var handle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;
        _handle = new(handle);

        // Add the hook to the window.
        var win = HwndSource.FromHwnd(handle);
        win.AddHook(WndProc);

        // Add clipboard format listener
        if (await RetryActionAsync(AddClipboardFormatListener))
        {
            Ready = true;
        }
    }

    /// <summary>
    /// Stops monitoring the system clipboard.
    /// </summary>
    public async void StopMonitoring()
    {
        if (await RetryActionAsync(RemoveClipboardFormatListener))
        {
            Ready = false;
        }
    }

    /// <summary>
    /// Add the clipboard format listener to the system clipboard.
    /// </summary>
    /// <returns>
    /// Returns true if the clipboard format listener was added successfully.
    /// </returns>
    private bool AddClipboardFormatListener()
    {
        if (_handle != HWND.Null)
        {
            var result = PInvoke.AddClipboardFormatListener(_handle);
            return result;
        }

        return false;
    }

    /// <summary>
    /// Retry an action asynchronously.
    /// </summary>
    /// <param name="action">
    /// The action to retry.
    /// </param>
    /// <param name="retryInterval">
    /// The interval between retries.
    /// </param>
    /// <param name="maxAttemptCount">
    /// The maximum count.
    /// </param>
    /// <returns>
    /// Returns a <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    private static async Task<bool> RetryActionAsync(Func<bool> action, int retryInterval = 100, int maxAttemptCount = 3)
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
                    return false;
                }

                await Task.Delay(retryInterval);
            }
        }

        return true;
    }

    /// <summary>
    /// Remove the clipboard format listener to the system clipboard.
    /// </summary>
    /// <returns>
    /// Returns true if the clipboard format listener was removed successfully.
    /// </returns>
    private bool RemoveClipboardFormatListener()
    {
        if (_handle != HWND.Null)
        {
            var result = PInvoke.RemoveClipboardFormatListener(_handle);
            _context.LogDebug(ClassName, "Clipboard format listener removed.");
            return result;
        }

        return false;
    }

    /// <summary>
    /// Handles the clipboard update event.
    /// </summary>
    /// <param name="hwnd"> Handle to the window that receives the message. </param>
    /// <param name="msg"> The message. </param>
    /// <param name="wParam"> Additional message information. </param>
    /// <param name="lParam"> Additional message information. </param>
    /// <param name="handled"> Whether the message was handled. </param>
    /// <returns></returns>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == PInvoke.WM_CLIPBOARDUPDATE)
        {
            OnClipboardChanged();
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Handles the clipboard data change event.
    /// </summary>
    private async void OnClipboardChanged()
    {
        try
        {
            // If clipboard-monitoring is enabled, proceed to listening.
            if ((!Ready) || ClipboardMonitorInstance == null || (!ClipboardMonitorInstance.MonitorClipboard))
            {
                return;
            }

            // Handle clipboard action in sta thread
            await Win32Helper.StartSTATaskAsync(() =>
            {
                // If the clipboard is empty, return.
                var dataObj = System.Windows.Clipboard.GetDataObject();
                if (dataObj is null)
                {
                    return;
                }

                // Determines whether a file/files have been cut/copied.
                if (ClipboardMonitorInstance.ObservableFormats.Images && IsDataImage(dataObj))
                {
                    // Make sure on the application dispatcher.
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (GetImageContent(dataObj) is BitmapSource capturedImage)
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
                    });
                }
                // Determines whether plain text or rich text has been cut/copied.
                else if (ClipboardMonitorInstance.ObservableFormats.Texts && IsDataText(dataObj))
                {
                    var (plainText, richText, dataType) = GetTextContent(dataObj);
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
                    if (GetFilesContent(dataObj) is string[] capturedFiles)
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
                        ClipboardMonitorInstance.ClipboardText = dataObj.GetData(DataFormats.UnicodeText) as string ?? string.Empty;

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
            });
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
        catch (COMException e) when (e.HResult == RPC_SERVER_UNAVAILABLE)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800706BA)
            // RPC server is unavailable (0x800706BA (RPC_E_SERVER_UNAVAILABLE))
        }
        catch (Exception e)
        {
            _context.LogException(ClassName, "Clipboard changed event failed.", e);
        }
    }

    #region Helper Methods

    public static bool IsDataImage(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.Bitmap);
    }

    public static bool IsDataText(IDataObject dataObj)
    {
        return IsDataAnsiText(dataObj) || IsDataUnicodeText(dataObj) || IsDataRichText(dataObj);
    }

    private static bool IsDataAnsiText(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.Text);
    }

    private static bool IsDataUnicodeText(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.UnicodeText);
    }

    private static bool IsDataRichText(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.Rtf);
    }

    public static bool IsDataFiles(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.FileDrop);
    }

    public static BitmapSource? GetImageContent(IDataObject dataObj)
    {
        if (dataObj.GetData(DataFormats.Bitmap) is BitmapSource capturedImage)
        {
            // Enable cross-thread access
            if (capturedImage.CanFreeze)
            {
                capturedImage.Freeze();
            }

            return capturedImage;
        }

        return null;
    }

    public static (string, string, DataType) GetTextContent(IDataObject dataObj)
    {
        var plainText = string.Empty;
        if (IsDataAnsiText(dataObj))
        {
            plainText = dataObj.GetData(DataFormats.Text) as string ?? string.Empty;
        }
        else if (IsDataUnicodeText(dataObj))
        {
            plainText = dataObj.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        }

        var richText = string.Empty;
        if (IsDataRichText(dataObj))
        {
            var capturedRtfData = dataObj.GetData(DataFormats.Rtf);
            if (capturedRtfData is string capturedRtfText)
            {
                richText = capturedRtfText;
            }
            else if (capturedRtfData is MemoryStream capturedRtfStream)
            {
                using var reader = new StreamReader(capturedRtfStream);
                capturedRtfText = reader.ReadToEnd();
                richText = capturedRtfText;
            }
        }

        return (plainText, richText, string.IsNullOrEmpty(richText) ? DataType.PlainText : DataType.RichText);
    }

    public static string[]? GetFilesContent(IDataObject dataObj)
    {
        if (dataObj.GetData(DataFormats.FileDrop) is string[] capturedFiles)
        {
            return capturedFiles;
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
