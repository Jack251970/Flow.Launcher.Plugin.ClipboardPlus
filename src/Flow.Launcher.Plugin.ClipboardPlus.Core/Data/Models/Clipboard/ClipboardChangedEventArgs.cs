// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

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
