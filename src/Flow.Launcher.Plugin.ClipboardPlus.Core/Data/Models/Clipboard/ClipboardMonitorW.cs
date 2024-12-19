// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardMonitorW is a class that monitors the clipboard
/// </summary>
public class ClipboardMonitorW : IDisposable
{
    #region Fields

    private static string ClassName => typeof(ClipboardMonitorW).Name;

    private PluginInitContext? _context;

    private DispatcherTimer _timer = new();
    private ClipboardHandleW _clipboardHandle = new();
    private ObservableDataFormats _observableFormats = new();

    private bool _monitorClipboard;
    private bool _observeLastEntry;
    private bool _startMonitoring;

    #endregion

    #region Properties

    #region Browsable

    public bool MonitorClipboard
    {
        get => _monitorClipboard;
        set => _monitorClipboard = value;
    }

    public bool ObserveLastEntry
    {
        get => _observeLastEntry;
        set => _observeLastEntry = value;
    }

    public ObservableDataFormats ObservableFormats
    {
        get => _observableFormats;
        set => _observableFormats = value;
    }

    #endregion

    #region Non-browsable

    public string ClipboardText { get; internal set; } = string.Empty;
    public string ClipboardRtfText { get; internal set; } = string.Empty;
    public object? ClipboardObject { get; internal set; } = null;
    public BitmapSource? ClipboardImage { get; internal set; }
    public string ClipboardFile { get; internal set; } = string.Empty;
    public List<string> ClipboardFiles { get; internal set; } = new();
    public static string HandleCaption { get; set; } = string.Empty;

    #endregion

    #endregion

    #region Constructors

    public ClipboardMonitorW()
    {
        _timer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 0, 0, 1000),
            IsEnabled = false
        };
        _timer.Tick += Timer_Tick;

        SetDefaults();
    }

    public void SetContext(PluginInitContext context)
    {
        _context = context;
        _clipboardHandle.SetContext(context);
    }

    #endregion

    #region Methods

    #region Public

    /// <summary>
    /// Starts the clipboard-monitoring process and
    /// initializes the system clipboard-access handle.
    /// </summary>
    public void StartMonitoring()
    {
        if (!_startMonitoring)
        {
            _timer.Start();
            _timer.IsEnabled = true;
        }
    }

    /// <summary>
    /// Pauses the clipboard-monitoring process.
    /// </summary>
    public void PauseMonitoring()
    {
        if (MonitorClipboard)
        {
            MonitorClipboard = false;
            _context?.API.LogDebug(ClassName, "Clipboard monitoring paused.");
        }
    }

    /// <summary>
    /// Resumes the clipboard-monitoring process.
    /// </summary>
    public void ResumeMonitoring()
    {
        if (!MonitorClipboard)
        {
            MonitorClipboard = true;
            _context?.API.LogDebug(ClassName, "Clipboard monitoring resumed.");
        }
    }

    /// <summary>
    /// Ends the clipboard-monitoring process and
    /// shuts the system clipboard-access handle.
    /// </summary>
    public void StopMonitoring()
    {
        if (_startMonitoring)
        {
            _clipboardHandle.StopMonitoring();
            _startMonitoring = false;
            _context?.API.LogDebug(ClassName, "Clipboard monitoring stopped.");
        }
    }

    #endregion

    #region Private

    /// <summary>
    /// Apply library-default settings and launch code.
    /// </summary>
    private void SetDefaults()
    {
        _clipboardHandle.ClipboardMonitorInstance = this;

        MonitorClipboard = true;
        ObserveLastEntry = true;
    }

    internal void Invoke(object? content, DataType type, SourceApplicationW source)
    {
        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(content, type, source));
    }

    #endregion

    #endregion

    #region Events

    #region Public

    #region Event Handlers

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged = null;

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
            SourceApplicationW source
        )
        {
            Content = content;
            DataType = dataType;

            SourceApplication = new SourceApplicationW(
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
        public SourceApplicationW SourceApplication { get; }

        #endregion
    }

    #endregion

    #endregion

    #region Private

    private void Timer_Tick(object? sender, EventArgs e)
    {
        // Wait until the dispatcher is ready & main window is initialized
        if (Application.Current.Dispatcher == null)
        {
            return;
        }
        else if (Application.Current.MainWindow == null)
        {
            return;
        }

        // Stop the timer & start monitoring
        _timer.Stop();
        _timer.IsEnabled = false;
        if (!_startMonitoring)
        {
            _clipboardHandle.StartMonitoring();
            _startMonitoring = true;
            _context?.API.LogDebug(ClassName, "Clipboard monitoring started.");
        }
    }

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
            _clipboardHandle.Dispose();
            _timer.Stop();
            _timer = null!;
            _clipboardHandle = null!;
            _observableFormats = null!;
            ClipboardFiles = null!;
            ClipboardImage = null!;
            _disposed = true;
        }
    }

    #endregion
}
