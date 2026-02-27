// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public interface IClipboardMonitor : IDisposable
{
    public bool MonitorClipboard { get; set; }

    public bool ObserveLastEntry { get; set; }

    public ObservableDataFormats ObservableFormats { get; set; }

    public string ClipboardText { get; }

    public string ClipboardRtfText { get; }

    public object? ClipboardObject { get; }

    public BitmapSource? ClipboardImage { get; }

    public string ClipboardFile { get; }

    public List<string> ClipboardFiles { get; }

    public void SetContext(PluginInitContext context);

    public void StartMonitoring();

    public void PauseMonitoring();

    public void ResumeMonitoring();

    public void StopMonitoring();

    public void CleanClipboard();

    public void AddExcludedPath(string path);

    public void RemoveExcludedPath(string path);

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
}
