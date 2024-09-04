// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

internal class ClipboardHandleW : IDisposable
{
    #region Fields

    const int WM_CLIPBOARDUPDATE = 0x031D;

    private HwndSource _hwndSource = null!;

    private bool _ready;

    private string _processName = string.Empty;
    private string _executableName = string.Empty;
    private string _executablePath = string.Empty;

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

    #region Clipboard Management

    #region Win32 Integration

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    #endregion

    #region Clipboard Monitor

    // The thread that monitors the system clipboard.
    // WPF requires the clipboard to be monitored on a separate thread.
    private Thread? staThread;
    private Dispatcher? dispatcher;

    /// <summary>
    /// Starts monitoring the system clipboard.
    /// </summary>
    public void StartMonitoring()
    {
        staThread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            CreateHiddenWindow();
            Dispatcher.Run(); // Start the message loop to keep the thread alive
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
    }

    /// <summary>
    /// Stops monitoring the system clipboard.
    /// </summary>
    public void StopMonitoring()
    {
        // Shut down the dispatcher to exit the message loop
        dispatcher?.InvokeShutdown();
        // Optionally, wait for the thread to exit
        if (staThread != null && staThread.IsAlive)
        {
            staThread.Join();
        }
        // Dispose of the hidden window
        if (_hwndSource is not null)
        {
            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.Dispose();
        }
    }

    /// <summary>
    /// Creates a hidden window to monitor the system clipboard.
    /// </summary>
    private void CreateHiddenWindow()
    {
        var parameters = new HwndSourceParameters(GetType().Name)
        {
            Width = 1,
            Height = 1,
            PositionX = -10000,
            PositionY = -10000,
            WindowStyle = unchecked((int)(0x80000000 | 0x10000000)), // WS_POPUP | WS_VISIBLE
            ExtendedWindowStyle = 0x00000080, // WS_EX_TOOLWINDOW
            ParentWindow = IntPtr.Zero,
            UsesPerPixelOpacity = false
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        AddClipboardFormatListener(_hwndSource.Handle);
        Ready = true;
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
        if (msg == WM_CLIPBOARDUPDATE)
        {
            OnClipboardChanged();
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Handles the clipboard data change event.
    /// </summary>
    private void OnClipboardChanged()
    {
        try
        {
            // If clipboard-monitoring is enabled, proceed to listening.
            if (!Ready || !ClipboardMonitorInstance.MonitorClipboard)
            {
                return;
            }

            // If the clipboard is empty, return.
            var dataObj = TaskUtils.Do(Clipboard.GetDataObject, 100, 5);
            if (dataObj is null)
            {
                return;
            }

            // Determines whether a file/files have been cut/copied.
            if (ClipboardMonitorInstance.ObservableFormats.Images && 
                dataObj.GetDataPresent(DataFormats.Bitmap))
            {
                // Because the clipboard is accessed on a separate thread,
                // the UI thread must be invoked to update the clipboard image.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var capturedImage = dataObj.GetData(DataFormats.Bitmap) as BitmapSource;
                    ClipboardMonitorInstance.ClipboardImage = capturedImage;

                    ClipboardMonitorInstance.Invoke(
                        capturedImage,
                        DataType.Image,
                        new SourceApplication(
                            GetForegroundWindow(),
                            ClipboardMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                });
            }
            // Determines whether unicode text or rich text has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Texts &&
                (dataObj.GetDataPresent(DataFormats.Text) ||
                dataObj.GetDataPresent(DataFormats.UnicodeText) ||
                dataObj.GetDataPresent(DataFormats.Rtf)))
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
                    ClipboardMonitorInstance.Invoke(
                        capturedText,
                        DataType.UnicodeText,
                        new SourceApplication(
                            GetForegroundWindow(),
                            ClipboardMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
                else
                {
                    ClipboardMonitorInstance.Invoke(
                        capturedRtfData,
                        DataType.RichText,
                        new SourceApplication(
                            GetForegroundWindow(),
                            ClipboardMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
            }
            // Determines whether a file has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Files && 
                dataObj.GetDataPresent(DataFormats.FileDrop))
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

                    ClipboardMonitorInstance.Invoke(
                        dataObj,
                        DataType.Other,
                        new SourceApplication(
                            GetForegroundWindow(),
                            ClipboardMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
                else
                {
                    // Clear all existing files before update.
                    ClipboardMonitorInstance.ClipboardFiles.Clear();
                    ClipboardMonitorInstance.ClipboardFiles.AddRange(capturedFiles);
                    ClipboardMonitorInstance.ClipboardFile = capturedFiles[0];

                    ClipboardMonitorInstance.Invoke(
                        capturedFiles,
                        DataType.Files,
                        new SourceApplication(
                            GetForegroundWindow(),
                            ClipboardMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
            }
            // Determines whether an unknown object has been cut/copied.
            else if (ClipboardMonitorInstance.ObservableFormats.Others &&
                !dataObj.GetDataPresent(DataFormats.FileDrop))
            {
                ClipboardMonitorInstance.Invoke(
                    dataObj,
                    DataType.Other,
                    new SourceApplication(
                        GetForegroundWindow(),
                        ClipboardMonitorInstance.ForegroundWindowHandle(),
                        GetApplicationName(),
                        GetActiveWindowTitle(),
                        GetApplicationPath()
                    )
                );
            }
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

    #endregion

    #endregion

    #region Souce App Management

    #region Win32 Externals

    [DllImport("user32.dll")]
    private static extern int GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindowPtr();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32")]
    private static extern UInt32 GetWindowThreadProcessId(Int32 hWnd, out Int32 lpdwProcessId);

    #endregion

    #region Helper Methods

    private Int32 GetProcessId(Int32 hwnd)
    {
        GetWindowThreadProcessId(hwnd, out var processId);
        return processId;
    }

    private string GetApplicationName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            _processName = Process.GetProcessById(GetProcessId(hwnd)).ProcessName;
            var processModule = Process.GetProcessById(GetProcessId(hwnd)).MainModule;
            if (processModule != null)
            {
                _executablePath = processModule.FileName;
            }
            _executableName = _executablePath[
                (_executablePath.LastIndexOf(@"\", StringComparison.Ordinal) + 1)..];
        }
        catch (Exception)
        {
            // ignored
        }

        return _executableName;
    }

    private string GetApplicationPath()
    {
        return _executablePath;
    }

    private string GetActiveWindowTitle()
    {
        const int capacity = 256;
        StringBuilder content = new(capacity);
        IntPtr handle = IntPtr.Zero;

        try
        {
            handle = ClipboardMonitorInstance.ForegroundWindowHandle();
        }
        catch (Exception)
        {
            // ignored
        }

        return GetWindowText(handle, content, capacity) > 0 ? content.ToString() : string.Empty;
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