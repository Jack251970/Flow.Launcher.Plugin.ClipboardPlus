/*
 * Basically CbMonitor.cs and CbHandle.cs are taken from SharpClipboard
 * with some modification, the original source code doesn't provide a
 * license, but MIT shown in nuget package so I copied them here.
 * Thanks for Willy-Kimura (https://github.com/Willy-Kimura/SharpClipboard)
 */

using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace ClipboardPlus.Core;

public class CbMonitor : IDisposable
{
    #region Fields

    private Timer _timer = new();
    private CbHandle _handle = new();
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

    public Image? ClipboardImage { get; internal set; }

    public static string HandleCaption { get; set; } = string.Empty;

    #endregion

    #endregion

    public CbMonitor()
    {
        SetDefaults();
    }

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
            ClipboardImage?.Dispose();
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
        _handle.CbMonitorInstance = this;

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
    static extern IntPtr GetForegroundWindow();

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
            DataType contentType,
            SourceApplication source
        )
        {
            Content = content;
            ContentType = contentType;

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
        public DataType ContentType { get; }

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

#region Property Classes

public class ObservableDataFormats
{
    /// <summary>
    /// Creates a new <see cref="ObservableDataFormats"/> options class-instance.
    /// </summary>
    public ObservableDataFormats()
    {
        _all = true;
    }

    #region Fields

    private bool _all;

    #endregion

    #region Properties

    public bool All
    {
        get => _all;
        set
        {
            _all = value;

            Texts = value;
            Files = value;
            Images = value;
            Others = value;
        }
    }

    public bool Texts { get; set; } = true;
    public bool Files { get; set; } = true;
    public bool Images { get; set; } = true;
    public bool Others { get; set; } = true;

    #endregion

    #region Overrides

    public override string ToString()
    {
        return $"Texts: {Texts}; Images: {Images}; Files: {Files}; Others: {Others}";
    }

    #endregion
}

public class SourceApplication
{
    /// <summary>
    /// Creates a new <see cref="SourceApplication"/> class-instance.
    /// </summary>
    /// <param name="id">The application's ID.</param>
    /// <param name="handle">The application's handle.</param>
    /// <param name="name">The application's name.</param>
    /// <param name="title">The application's title.</param>
    /// <param name="path">The application's path.</param>
    internal SourceApplication(int id, IntPtr handle, string name, string title, string path)
    {
        Id = id;
        Name = name;
        Path = path;
        Title = title;
        Handle = handle;
    }

    #region Properties

    /// <summary>
    /// Gets the application's process-ID.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the appliation's window-handle.
    /// </summary>
    public IntPtr Handle { get; }

    /// <summary>
    /// Gets the application's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the application's title-text.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the application's absolute path.
    /// </summary>
    public string Path { get; }

    #endregion

    #region Overrides

    /// <summary>
    /// Returns a <see cref="string"/> containing the list
    /// of application details provided.
    /// </summary>
    public override string ToString()
    {
        return $"ID: {Id}; Handle: {Handle}, Name: {Name}; " + $"Title: {Title}; Path: {Path}";
    }

    #endregion
}

#endregion
