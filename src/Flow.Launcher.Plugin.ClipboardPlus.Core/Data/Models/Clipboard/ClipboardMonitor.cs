/*
 * ClipboardMonitor.cs is from https://github.com/Willy-Kimura/SharpClipboard
 * with some modification, the original source code doesn't provide a
 * license, but MIT license shown in nuget package so I copied them here
 */

using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Timer = System.Windows.Forms.Timer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class ClipboardMonitor : IDisposable
{
    #region Fields

    private Timer _timer = new();
    private ClipboardHandle _handle = new();
    private ObservableDataFormats _observableFormats = new();

    private bool _monitorClipboard;
    private bool _observeLastEntry;

    #endregion

    #region Properties

    #region Browsable

    public bool MonitorClipboard
    {
        get => _monitorClipboard;
        set
        {
            _monitorClipboard = value;
            MonitorClipboardChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool ObserveLastEntry
    {
        get => _observeLastEntry;
        set
        {
            _observeLastEntry = value;
            ObserveLastEntryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ObservableDataFormats ObservableFormats
    {
        get => _observableFormats;
        set
        {
            _observableFormats = value;
            ObservableFormatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Non-browsable

    public string ClipboardText { get; internal set; } = string.Empty;

    public object? ClipboardObject { get; internal set; }

    public string ClipboardFile { get; internal set; } = string.Empty;

    public List<string> ClipboardFiles { get; internal set; } = new();

    public BitmapSource? ClipboardImage { get; internal set; }

    public static string HandleCaption { get; set; } = string.Empty;

    #endregion

    #endregion

    #region Constructors

    public ClipboardMonitor()
    {
        SetDefaults();
    }

    #endregion

    #region Methods

    #region Public

    /// <summary>
    /// Gets the current foreground window's handle.
    /// </summary>
    /// <returns>
    /// Handle to the currently active window.
    /// </returns>
    public IntPtr ForegroundWindowHandle()
    {
        return GetForegroundWindow();
    }

    /// <summary>
    /// Starts the clipboard-monitoring process and
    /// initializes the system clipboard-access handle.
    /// </summary>
    public void StartMonitoring()
    {
        _handle.Show();
    }

    /// <summary>
    /// Ends the clipboard-monitoring process and
    /// shuts the system clipboard-access handle.
    /// </summary>
    public void StopMonitoring()
    {
        _handle.Close();
    }

    #region IDisposable Interface

    /// <summary>
    /// Disposes of the clipboard-monitoring resources.
    /// </summary>
    public void Dispose()
    {
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
            _timer.Dispose();
            _timer = null!;
            _handle.Dispose();
            _handle = null!;
            _observableFormats = null!;
            ClipboardFiles = null!;
            ClipboardImage = null!;
        }
    }

    #endregion

    #endregion

    #region Private

    /// <summary>
    /// Apply library-default settings and launch code.
    /// </summary>
    private void SetDefaults()
    {
        _handle.ClipboardMonitorInstance = this;

        _timer.Enabled = true;
        _timer.Interval = 1000;
        _timer.Tick += OnLoad;

        MonitorClipboard = true;
        ObserveLastEntry = true;
    }

    internal void Invoke(object? content, DataType type, SourceApplication source)
    {
        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(content, type, source));
    }

    /// <summary>
    /// Gets the foreground or currently active window handle.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    #endregion

    #endregion

    #region Events

    #region Public

    #region Event Handlers

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged = null;

    public event EventHandler<EventArgs>? MonitorClipboardChanged = null;

    public event EventHandler<EventArgs>? ObservableFormatsChanged = null;

    public event EventHandler<EventArgs>? ObserveLastEntryChanged = null;

    #endregion

    #region Event Arguments

    /// <summary>
    /// Provides data for the <see cref="ClipboardChanged"/> event.
    /// </summary>
    public class ClipboardChangedEventArgs : EventArgs
    {
        public ClipboardChangedEventArgs(
            object? content,
            DataType dataType,
            SourceApplication source
        )
        {
            Content = content;
            DataType = dataType;

            SourceApplication = new SourceApplication(
                source.Id,
                source.Handle,
                source.Name,
                source.Title,
                source.Path
            );
        }

        #region Properties

        /// <summary>
        /// Gets the currently copied clipboard content.
        /// </summary>
        public object? Content { get; }

        /// <summary>
        /// Gets the currently copied clipboard content-type.
        /// </summary>
        public DataType DataType { get; }

        /// <summary>
        /// Gets the application from where the
        /// clipboard's content were copied.
        /// </summary>
        public SourceApplication SourceApplication { get; }

        #endregion
    }

    #endregion

    #endregion

    #region Private

    /// <summary>
    /// This initiates a Timer that then begins the
    /// clipboard-monitoring service. The Timer will
    /// auto-shutdown once the service has started.
    /// </summary>
    private void OnLoad(object? sender, EventArgs e)
    {
        _timer.Stop();
        _timer.Enabled = false;
        StartMonitoring();
    }

    #endregion

    #endregion
}
