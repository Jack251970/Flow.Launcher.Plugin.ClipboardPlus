﻿// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using Windows.Win32.Foundation;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SourceApplication
{
    #region Properties

    /// <summary>
    /// Gets a <see cref="SourceApplication"/> instance representing a null-application.
    /// </summary>
    public static SourceApplication NULL => new(HWND.Null, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the appliation's window-handle.
    /// </summary>
    internal HWND Handle { get; }

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

    #region Constructor

    /// <summary>
    /// Creates a new <see cref="SourceApplication"/> class-instance.
    /// </summary>
    /// <param name="handle">The application's handle.</param>
    /// <param name="name">The application's name.</param>
    /// <param name="title">The application's title.</param>
    /// <param name="path">The application's path.</param>
    internal SourceApplication(HWND handle, string name, string title, string path)
    {
        Handle = handle;
        Name = name;
        Path = path;
        Title = title;
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Returns a <see cref="string"/> containing the list
    /// of application details provided.
    /// </summary>
    public override string ToString()
    {
        return $"Handle: {Handle}, Name: {Name}; " + $"Title: {Title}; Path: {Path}";
    }

    #endregion
}
