/*
 * SourceApplication.cs is from https://github.com/Willy-Kimura/SharpClipboard
 * with some modification, the original source code doesn't provide a
 * license, but MIT license shown in nuget package so I copied them here
 */

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

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
