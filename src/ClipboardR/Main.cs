using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowsInput;
using WK.Libraries.SharpClipboardNS;
using ClipboardR.Core;
using ClipboardR.Panels;

namespace ClipboardR;

public class ClipboardR : IPlugin, IDisposable, ISettingProvider, ISavable
{
    private SharpClipboard _clipboard = new() { ObserveLastEntry = false };
    private DirectoryInfo ClipDir { get; set; } = null!;
    private DirectoryInfo ClipCacheDir { get; set; } = null!;
    private string _defaultIconPath = null!;
    private const int MaxDataCount = 1000;
    private const string PinUnicode = "📌";
    private Settings _settings = null!;
    private string _settingsPath = null!;
    private int CurrentScore { get; set; } = 0;

    // private readonly InputSimulator inputSimulator = new InputSimulator();
    private PluginInitContext? _context;
    private LinkedList<ClipboardData> _dataList = new();

    public void Init(PluginInitContext ctx)
    {
        this._context = ctx;
        this._clipboard.ClipboardChanged += _OnClipboardChange;

        ClipDir = new DirectoryInfo(ctx.CurrentPluginMetadata.PluginDirectory);
        var imageCacheDirectoryPath = Path.Combine(ClipDir.FullName, "CachedImages");
        ClipCacheDir = !Directory.Exists(imageCacheDirectoryPath)
            ? Directory.CreateDirectory(imageCacheDirectoryPath)
            : new DirectoryInfo(imageCacheDirectoryPath);

        _defaultIconPath = Path.Join(ClipDir.FullName, "Images/clipboard.png");

        _settingsPath = Path.Join(ClipDir.FullName, "settings.json");
        if (File.Exists(_settingsPath))
        {
            using var fs = File.OpenRead(_settingsPath);
            _settings = JsonSerializer.Deserialize<Settings>(fs)!;
        }
        else
            _settings = new Settings();

        _settings.ConfigFile = _settingsPath;
        _settings.Save();
    }

    public List<Result> Query(Query query)
    {
        var displayData = query.Search.Trim().Length == 0
            ? _dataList.ToArray()
            : _dataList.Where(i =>
                !string.IsNullOrEmpty(i.Text) && i.Text.ToLower().Contains(query.Search.Trim().ToLower())).ToArray();

        var results = new List<Result>();
        results.AddRange(displayData.Where(cd => cd.Pined).Select(ClipDataToResult));
        results.AddRange(displayData.Where(cd => !cd.Pined).Select(ClipDataToResult));
        return results;
    }

    private Result ClipDataToResult(ClipboardData o)
    {
        var disSubTitle = o.Pined ? $"{PinUnicode}: {o.SenderApp}" : o.SenderApp!;
        return new Result
        {
            Title = o.DisplayTitle,
            SubTitle = disSubTitle,
            Icon = () => o.Icon,
            CopyText = o.Text,
            Score = o.Score,
            TitleToolTip = o.Text,
            SubTitleToolTip = disSubTitle,
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(
                o,
                _context!,
                ClipCacheDir,
                delAction: RemoveFromDatalist,
                copyAction: CopyToClipboard,
                pinAction: PinOneRecord
            )),
            AsyncAction = async _ =>
            {
                CopyToClipboard(o);
                _context!.API.HideMainWindow();
                while (_context!.API.IsMainWindowVisible())
                    await Task.Delay(100);
                new InputSimulator()
                    .Keyboard
                    .ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                return true;
            },
        };
    }

    private void _OnClipboardChange(object? sender, SharpClipboard.ClipboardChangedEventArgs e)
    {
        if (e.Content is null) return;
        ClipboardData clipboardData = new ClipboardData
        {
            Text = "",
            Type = e.ContentType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = CurrentScore + 1,
            InitScore = CurrentScore + 1,
            Time = DateTime.Now,
            Pined = false,
        };
        switch (e.ContentType)
        {
            case SharpClipboard.ContentTypes.Text:
                clipboardData.Text = _clipboard.ClipboardText;
                break;
            case SharpClipboard.ContentTypes.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (_settings.CacheImages) Utils.SaveImageCache(clipboardData, ClipCacheDir);
                clipboardData.Icon = _clipboard.ClipboardImage.ToBitmapImage();

                break;
            case SharpClipboard.ContentTypes.Files:
                var t = _clipboard.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                break;
            case SharpClipboard.ContentTypes.Other:
                // TODO: nothing to do now
                return;
            default:
                break;
        }

        clipboardData.DisplayTitle = Regex.Replace(clipboardData.Text.Trim(), @"(\r|\n|\t|\v)", "");

        // make sure no repeat
        if (_dataList.Any(node => node.Equals(clipboardData)))
            return;
        _dataList.AddFirst(clipboardData);
        if (_dataList.Count > MaxDataCount)
            _dataList.RemoveLast();
        CurrentScore++;
    }

    public Control CreateSettingPanel() => new SettingsPanel(_settings, _context!);

    public void Dispose()
    {
    }

    public void CopyToClipboard(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void RemoveFromDatalist(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void PinOneRecord(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        _dataList.AddLast(clipboardData);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    // private Task<bool> HideMainWindow()
    // {
    //     
    // }

    public void Save()
    {
        _settings?.Save();
    }
}