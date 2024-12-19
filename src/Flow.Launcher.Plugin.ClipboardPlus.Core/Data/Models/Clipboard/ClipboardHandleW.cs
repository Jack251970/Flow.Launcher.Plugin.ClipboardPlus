// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

internal class ClipboardHandleW : IDisposable
{
    #region Fields

    private static string ClassName => typeof(ClipboardHandleW).Name;

    private PluginInitContext? _context;

    private HWND _handle = HWND.Null;

    private bool _ready;

    private IntPtr _executableHandle = IntPtr.Zero;
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
    internal ClipboardMonitorW ClipboardMonitorInstance { get; set; } = null!;

    #endregion

    #region Constructor

    public ClipboardHandleW()
    {

    }

    public void SetContext(PluginInitContext context)
    {
        _context = context;
    }

    #endregion

    #region Methods

    #region Clipboard Management

    #region Clipboard Monitor

    /// <summary>
    /// Starts monitoring the system clipboard.
    /// </summary>
    public void StartMonitoring()
    {
        if (Application.Current.MainWindow.IsLoaded)
        {
            MainWindow_Loaded(null, new RoutedEventArgs());
        }
        else
        {
            Application.Current.MainWindow.Loaded += MainWindow_Loaded;
        }
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Get the handle of the main window.
        var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
        _handle = new(handle);

        // Add the hook to the window.
        var win = HwndSource.FromHwnd(handle);
        win.AddHook(WndProc);

        // Add clipboard format listener
        await RetryActionAsync(AddClipboardFormatListener);

        Ready = true;
    }

    /// <summary>
    /// Stops monitoring the system clipboard.
    /// </summary>
    public async void StopMonitoring()
    {
        await RetryActionAsync(RemoveClipboardFormatListener);
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
            _context?.API.LogDebug(ClassName, "Clipboard format listener added.");
            return result;
        }
        return false;
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
            _context?.API.LogDebug(ClassName, "Clipboard format listener removed.");
            return result;
        }
        return true;
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
            if (!Ready || !ClipboardMonitorInstance.MonitorClipboard)
            {
                return;
            }

            // Handle clipboard action in sta thread
            await Win32Helper.StartSTATaskAsync(() =>
            {
                // If the clipboard is empty, return.
                var dataObj = Clipboard.GetDataObject();
                if (dataObj is null)
                {
                    return;
                }

                // Determines whether a file/files have been cut/copied.
                if (ClipboardMonitorInstance.ObservableFormats.Images && IsDataImage(dataObj))
                {
                    // Make sure on the application dispatcher.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (dataObj.GetData(DataFormats.Bitmap) is BitmapSource capturedImage)
                        {
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
                                    new SourceApplicationW(
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
                // Determines whether unicode text or rich text has been cut/copied.
                else if (ClipboardMonitorInstance.ObservableFormats.Texts && IsDataText(dataObj))
                {
                    var capturedText = dataObj.GetData(DataFormats.UnicodeText) as string;
                    var capturedRtfData = dataObj.GetData(DataFormats.Rtf);

                    var unicodeText = false;
                    if (capturedRtfData is string capturedRtfText)
                    {
                        ClipboardMonitorInstance.ClipboardRtfText = capturedRtfText;
                    }
                    else if (capturedRtfData is MemoryStream capturedRtfStream)
                    {
                        using var reader = new StreamReader(capturedRtfStream);
                        capturedRtfText = reader.ReadToEnd();
                        ClipboardMonitorInstance.ClipboardRtfText = capturedRtfText;
                    }
                    else
                    {
                        ClipboardMonitorInstance.ClipboardRtfText = string.Empty;
                        unicodeText = true;
                    }
                    ClipboardMonitorInstance.ClipboardText = capturedText ?? string.Empty;

                    if (unicodeText)
                    {
                        if (GetApplicationInfo())
                        {
                            ClipboardMonitorInstance.Invoke(
                                capturedText,
                                DataType.UnicodeText,
                                new SourceApplicationW(
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
                        if (GetApplicationInfo())
                        {
                            ClipboardMonitorInstance.Invoke(
                                capturedRtfData,
                                DataType.RichText,
                                new SourceApplicationW(
                                    _executableHandle,
                                    _executableName,
                                    _executableTitle,
                                    _executablePath
                                )
                            );
                        }
                    }
                }
                // Determines whether a file has been cut/copied.
                else if (ClipboardMonitorInstance.ObservableFormats.Files && IsDataFiles(dataObj))
                {
                    // If the 'capturedFiles' string array persists as null, then this means
                    // that the copied content is of a complex object type since the file-drop
                    // format is able to capture more-than-just-file content in the clipboard.
                    // Therefore assign the content its rightful type.
                    if (dataObj.GetData(DataFormats.FileDrop) is not string[] capturedFiles)
                    {
                        ClipboardMonitorInstance.ClipboardObject = dataObj;
                        var txt = dataObj.GetData(DataFormats.UnicodeText) as string;
                        ClipboardMonitorInstance.ClipboardText = txt ?? string.Empty;

                        if (GetApplicationInfo())
                        {
                            ClipboardMonitorInstance.Invoke(
                                dataObj,
                                DataType.Other,
                                new SourceApplicationW(
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
                        ClipboardMonitorInstance.ClipboardFiles.Clear();
                        ClipboardMonitorInstance.ClipboardFiles.AddRange(capturedFiles);
                        ClipboardMonitorInstance.ClipboardFile = capturedFiles[0];

                        if (GetApplicationInfo())
                        {
                            ClipboardMonitorInstance.Invoke(
                                capturedFiles,
                                DataType.Files,
                                new SourceApplicationW(
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
                            new SourceApplicationW(
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
        catch (NullReferenceException) {}
        catch (COMException)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800401D0)
            // OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))
        }
    }

    private static bool IsDataImage(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.Bitmap);
    }

    private static bool IsDataText(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.Text) ||
            dataObj.GetDataPresent(DataFormats.UnicodeText) ||
            dataObj.GetDataPresent(DataFormats.Rtf);
    }

    private static bool IsDataFiles(IDataObject dataObj)
    {
        return dataObj.GetDataPresent(DataFormats.FileDrop);
    }


    #endregion

    #endregion

    #region Souce App Management

    #region Helper Methods

    private unsafe bool GetApplicationInfo()
    {
        _executableHandle = IntPtr.Zero;
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
