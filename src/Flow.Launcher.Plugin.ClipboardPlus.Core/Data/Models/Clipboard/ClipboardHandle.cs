/*
 * ClipboardHandle.cs is from https://github.com/Willy-Kimura/SharpClipboard
 * with some modification, the original source code doesn't provide a
 * license, but MIT license shown in nuget package so I copied them here
 */

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public partial class ClipboardHandle : Form
{
    #region Constructor

    public ClipboardHandle()
    {
        InitializeComponent();

        // [optional] Applies the default window title.
        // This may only be necessary for forensic purposes.
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        Visible = false;
        Opacity = 0d;
        Size = new Size(1, 1);
        Location = new Point(-10000, -10000);
    }

    #endregion

    #region Fields

    const int WM_CLIPBOARDUPDATE = 0x031D;

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
    internal ClipboardMonitor ClipboardMonitorInstance { get; set; } = null!;

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

    /// <summary>
    /// Modifications in this overriden method have
    /// been added to disable viewing of the handle-
    /// window in the Task Manager.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // Turn on WS_EX_TOOLWINDOW.
            cp.ExStyle |= 0x80;
            return cp;
        }
    }

    /// <summary>
    /// This is the main clipboard detection method.
    /// Algorithmic customizations are most welcome.
    /// </summary>
    /// <param name="m">The processed window-reference message.</param>
    protected override void WndProc(ref Message m)
    {
        Console.WriteLine(m.ToString());
        switch (m.Msg)
        {
            case WM_CLIPBOARDUPDATE:
                OnDrawClipboardChanged();
                break;
            default:
                base.WndProc(ref m);
                break;
        }
    }

    private void OnDrawClipboardChanged()
    {
        try
        {
            // If clipboard-monitoring is enabled, proceed to listening.
            if (!Ready || !ClipboardMonitorInstance.MonitorClipboard)
            {
                return;
            }
            var dataObj = TaskUtils.Do(Clipboard.GetDataObject, 100, 5);
            if (dataObj is null)
            {
                return;
            }

            // Determines whether a file/files have been cut/copied.
            if (
                ClipboardMonitorInstance.ObservableFormats.Images
                && dataObj.GetDataPresent(DataFormats.Bitmap)
            )
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
            }
            // Determines whether text has been cut/copied.
            else if (
                ClipboardMonitorInstance.ObservableFormats.Texts
                && (
                    dataObj.GetDataPresent(DataFormats.Text)
                    || dataObj.GetDataPresent(DataFormats.UnicodeText)
                )
            )
            {
                var capturedText = dataObj.GetData(DataFormats.UnicodeText) as string;
                ClipboardMonitorInstance.ClipboardText = capturedText ?? "";

                ClipboardMonitorInstance.Invoke(
                    capturedText,
                    DataType.Text,
                    new SourceApplication(
                        GetForegroundWindow(),
                        ClipboardMonitorInstance.ForegroundWindowHandle(),
                        GetApplicationName(),
                        GetActiveWindowTitle(),
                        GetApplicationPath()
                    )
                );
            }
            else if (
                ClipboardMonitorInstance.ObservableFormats.Files
                && dataObj.GetDataPresent(DataFormats.FileDrop)
            )
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
            // Determines whether a complex object has been cut/copied.
            else if (
                ClipboardMonitorInstance.ObservableFormats.Others
                && !dataObj.GetDataPresent(DataFormats.FileDrop)
            )
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
        catch (NullReferenceException) { }
        catch (COMException)
        {
            // Sometimes the clipboard is locked and cannot be accessed.
            // System.Runtime.InteropServices.COMException (0x800401D0)
            // OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))
        }
    }

    #endregion

    #region Helper Methods

    public void StartMonitoring()
    {
        Show();
    }

    public void StopMonitoring()
    {
        Close();
    }

    #endregion

    #endregion

    #region Source App Management

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
        StringBuilder content = new StringBuilder(capacity);
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

    #region Events

    protected override void OnHandleCreated(EventArgs e)
    {
        // Start listening for clipboard changes.
        TaskUtils.SafeDo(() => AddClipboardFormatListener(Handle), 100, 5);
        Ready = true;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        // Stop listening to clipboard changes.
        TaskUtils.SafeDo(() => RemoveClipboardFormatListener(Handle), 100, 5);
        base.OnHandleDestroyed(e);
    }

    #endregion
}
