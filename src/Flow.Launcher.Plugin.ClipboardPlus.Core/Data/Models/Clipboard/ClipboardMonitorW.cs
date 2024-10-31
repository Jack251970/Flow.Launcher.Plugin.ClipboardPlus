// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardMonitorW is a class that monitors the clipboard
/// </summary>
public class ClipboardMonitorW : IDisposable
{
    #region Fields

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
        if (!_startMonitoring)
        {
            _clipboardHandle.StartMonitoring();
            _startMonitoring = true;
        }
    }

    /// <summary>
    /// Pauses the clipboard-monitoring process.
    /// </summary>
    public void PauseMonitoring()
    {
        MonitorClipboard = false;
    }

    /// <summary>
    /// Resumes the clipboard-monitoring process.
    /// </summary>
    public void ResumeMonitoring()
    {
        MonitorClipboard = true;
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

    internal void Invoke(object? content, DataType type, SourceApplication source)
    {
        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(content, type, source));
    }

    #region Win32 Interop

    [DllImport("user32.dll")]
    private static extern int GetForegroundWindow();

    #endregion

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
            _clipboardHandle = null!;
            _observableFormats = null!;
            ClipboardFiles = null!;
            ClipboardImage = null!;
            _disposed = true;
        }
    }

    #endregion
}
