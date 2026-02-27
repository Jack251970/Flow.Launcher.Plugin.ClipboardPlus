using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class ClipboardPlus : IClipboardPlus, IAsyncDisposable
{
    public PluginInitContext? Context { get; private set; } = new(null, Ioc.Default.GetRequiredService<IPublicAPI>());

    public bool UseWindowsClipboardHistoryOnly => false;

    public ObservableDataFormats ObservableDataFormats { get; private set; } = new()
    {
        Images = true,
        Texts = true,
        Files = true,
        Others = false
    };

    public List<IClipboardMonitor> ClipboardMonitors { get; private set; } = [];

    public SqliteDatabase Database { get; private set; }

    public ScoreHelper ScoreHelper { get; } = new ScoreHelper(1);

    public ISettings Settings { get; }

    public CultureInfo CultureInfo { get; private set; } = new CultureInfo("en-US");

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public ClipboardPlus()
    {
        Settings = new Settings();
        StringUtils.InitEncryptKey(Settings.EncryptKey);
        Database = new SqliteDatabase(Path.Combine(AppContext.BaseDirectory, "ClipboardPlus.db"), this);
    }

    public void EnableWindowsClipboardHelper(bool load)
    {

    }

    public void DisableWindowsClipboardHelper(bool remove)
    {

    }

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText)
    {
        return ClipboardData.NULL;
    }

    public async Task InitRecordsFromDatabaseAndSystemAsync(bool database, bool system)
    {
        await Task.CompletedTask;
    }

    public ISettings LoadSettingJsonStorage()
    {
        return Settings;
    }

    public void OnCultureInfoChanged(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        CultureInfoChanged?.Invoke(this, cultureInfo);
    }

    public void SaveSettingJsonStorage()
    {

    }

    public void AddExcludedPath(string appPath)
    {
        foreach (var clipboardMonitor in ClipboardMonitors)
            clipboardMonitor.AddExcludedPath(appPath);
    }

    public void RemoveExcludedPath(string appPath)
    {
        foreach (var clipboardMonitor in ClipboardMonitors)
            clipboardMonitor.RemoveExcludedPath(appPath);
    }

    public void ClearExcludedPath()
    {
        foreach (var clipboardMonitor in ClipboardMonitors)
            clipboardMonitor.ClearExcludedPath();
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await Database.DisposeAsync();
            Database = null!;
            GarbageCollect();
            _disposed = true;
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
