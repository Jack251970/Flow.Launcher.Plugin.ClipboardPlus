// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

/// <summary>
/// ClipboardMonitorW is a class that monitors the clipboard
/// </summary>
[SupportedOSPlatform("windows6.0.6000")]
public class ClipboardMonitorW : IClipboardMonitor
{
    #region Fields

    private static string ClassName => nameof(ClipboardMonitorW);

    private PluginInitContext? _context;

    private DispatcherTimer _timer = new();
    private ClipboardHandleW _clipboardHandle = new();
    private ObservableDataFormats _observableFormats = new();
    private readonly List<string> _excludedPaths = [];

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
    public List<string> ClipboardFiles { get; internal set; } = [];

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
            _context.LogDebug(ClassName, "Clipboard monitoring paused.");
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
            _context.LogDebug(ClassName, "Clipboard monitoring resumed.");
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
            _context.LogDebug(ClassName, "Clipboard monitoring stopped.");
        }
    }

    /// <summary>
    /// Clears the clipboard of all data.
    /// </summary>
    public void CleanClipboard()
    {
        ClipboardText = string.Empty;
        ClipboardRtfText = string.Empty;
        ClipboardObject = null;
        ClipboardImage = null;
        ClipboardFile = string.Empty;
        ClipboardFiles.Clear();
    }

    /// <summary>
    /// Adds a file system path to the collection of excluded paths.
    /// </summary>
    /// <remarks>Use this method to prevent the specified path from being processed or included in operations
    /// that consider excluded paths. Paths added are not validated for existence.</remarks>
    /// <param name="path">The file system path to exclude. Cannot be null or empty.</param>
    public void AddExcludedPath(string path)
    {
        lock (_excludedPaths)
        {
            _excludedPaths.Add(path);
        }
    }

    /// <summary>
    /// Removes the specified path from the collection of excluded paths, if it exists.
    /// </summary>
    /// <param name="path">The path to remove from the excluded paths collection. Cannot be null or empty.</param>
    public void RemoveExcludedPath(string path)
    {
        lock (_excludedPaths)
        {
            _excludedPaths.Remove(path);
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

    internal bool IsPathExcluded(string path)
    {
        lock (_excludedPaths)
        {
            foreach (var item in _excludedPaths)
            {
                if (string.Equals(item, path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    #endregion

    #endregion

    #region Events

    #region Public

    #region Event Handlers

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged = null;

    #endregion

    #endregion

    #region Private

    private void Timer_Tick(object? sender, EventArgs e)
    {
        // Wait until the dispatcher is ready & main window is initialized
        if (System.Windows.Application.Current.Dispatcher == null)
        {
            return;
        }
        else if (System.Windows.Application.Current.MainWindow == null)
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
            CleanClipboard();
            _disposed = true;
        }
    }

    #endregion
}
