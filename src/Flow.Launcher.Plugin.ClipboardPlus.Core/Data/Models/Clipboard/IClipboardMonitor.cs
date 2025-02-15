// Copyright (c) 2024 Jack251970
// Licensed under the Apache License. See the LICENSE.

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

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
}
