using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipboardPlus.Core;

public partial class CbHandle : Form
{
    #region Constructor

    public CbHandle()
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
            if (CbMonitorInstance.ObserveLastEntry)
                _ready = true;
            return _ready;
        }
        set => _ready = value;
    }

    // instant in monitor
    internal CbMonitor CbMonitorInstance { get; set; } = null!;

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
        // If clipboard-monitoring is enabled, proceed to listening.
        if (!Ready || !CbMonitorInstance.MonitorClipboard)
        {
            return;
        }
        var dataObj = TaskUtils.Do(Clipboard.GetDataObject, 100, 5);
        if (dataObj is null)
        {
            return;
        }
        try
        {
            // Determines whether a file/files have been cut/copied.
            if (
                CbMonitorInstance.ObservableFormats.Images
                && dataObj.GetDataPresent(DataFormats.Bitmap)
            )
            {
                var capturedImage = dataObj.GetData(DataFormats.Bitmap) as Image;
                CbMonitorInstance.ClipboardImage = capturedImage;

                CbMonitorInstance.Invoke(
                    capturedImage,
                    CbContentType.Image,
                    new SourceApplication(
                        GetForegroundWindow(),
                        CbMonitorInstance.ForegroundWindowHandle(),
                        GetApplicationName(),
                        GetActiveWindowTitle(),
                        GetApplicationPath()
                    )
                );
            }
            // Determines whether text has been cut/copied.
            else if (
                CbMonitorInstance.ObservableFormats.Texts
                && (
                    dataObj.GetDataPresent(DataFormats.Text)
                    || dataObj.GetDataPresent(DataFormats.UnicodeText)
                )
            )
            {
                var capturedText = dataObj.GetData(DataFormats.UnicodeText) as string;
                CbMonitorInstance.ClipboardText = capturedText ?? "";

                CbMonitorInstance.Invoke(
                    capturedText,
                    CbContentType.Text,
                    new SourceApplication(
                        GetForegroundWindow(),
                        CbMonitorInstance.ForegroundWindowHandle(),
                        GetApplicationName(),
                        GetActiveWindowTitle(),
                        GetApplicationPath()
                    )
                );
            }
            else if (
                CbMonitorInstance.ObservableFormats.Files
                && dataObj.GetDataPresent(DataFormats.FileDrop)
            )
            {
                // If the 'capturedFiles' string array persists as null, then this means
                // that the copied content is of a complex object type since the file-drop
                // format is able to capture more-than-just-file content in the clipboard.
                // Therefore assign the content its rightful type.
                if (dataObj.GetData(DataFormats.FileDrop) is not string[] capturedFiles)
                {
                    CbMonitorInstance.ClipboardObject = dataObj;
                    var txt = dataObj.GetData(DataFormats.UnicodeText) as string;
                    CbMonitorInstance.ClipboardText = txt ?? "";

                    CbMonitorInstance.Invoke(
                        dataObj,
                        CbContentType.Other,
                        new SourceApplication(
                            GetForegroundWindow(),
                            CbMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
                else
                {
                    // Clear all existing files before update.
                    CbMonitorInstance.ClipboardFiles.Clear();
                    CbMonitorInstance.ClipboardFiles.AddRange(capturedFiles);
                    CbMonitorInstance.ClipboardFile = capturedFiles[0];

                    CbMonitorInstance.Invoke(
                        capturedFiles,
                        CbContentType.Files,
                        new SourceApplication(
                            GetForegroundWindow(),
                            CbMonitorInstance.ForegroundWindowHandle(),
                            GetApplicationName(),
                            GetActiveWindowTitle(),
                            GetApplicationPath()
                        )
                    );
                }
            }
            // Determines whether a complex object has been cut/copied.
            else if (
                CbMonitorInstance.ObservableFormats.Others
                && !dataObj.GetDataPresent(DataFormats.FileDrop)
            )
            {
                CbMonitorInstance.Invoke(
                    dataObj,
                    CbContentType.Other,
                    new SourceApplication(
                        GetForegroundWindow(),
                        CbMonitorInstance.ForegroundWindowHandle(),
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
            _executableName = _executablePath.Substring(
                _executablePath.LastIndexOf(@"\", StringComparison.Ordinal) + 1
            );
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
            handle = CbMonitorInstance.ForegroundWindowHandle();
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
        TaskUtils.Do(() => AddClipboardFormatListener(Handle), 100, 5);
        Ready = true;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        // Stop listening to clipboard changes.
        TaskUtils.Do(() => RemoveClipboardFormatListener(Handle), 100, 5);
        base.OnHandleDestroyed(e);
    }

    #endregion
}
