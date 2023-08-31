using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsInput;
using ClipboardR.Core;
using ClipboardR.Panels;
using Material.Icons;
using Material.Icons.WPF;

namespace ClipboardR;

public class ClipboardR : IPlugin, IDisposable, ISettingProvider, ISavable
{
    // clipboard listener instance
    private CbMonitor _clipboard = new() { ObserveLastEntry = false };
    private string ClassName => GetType().Name;

    // working directory
    private DirectoryInfo ClipDir { get; set; } = null!;

    private DirectoryInfo ClipCacheDir { get; set; } = null!;

    // default icon path
    private string _defaultIconPath = null!;
    private string _defaultPinIconPath = null!;

    private string _settingsPath = null!;

    // max data count, will be rewritten by settings
    private int _maxDataCount = 10000;
    private const string PinUnicode = "📌";
    private Settings _settings = null!;
    private int CurrentScore { get; set; } = 1;

    private DbHelper _dbHelper = null!;
    private string _dbPath = null!;

    private PluginInitContext _context = null!;
    private LinkedList<ClipboardData> _dataList = new();

    public void Init(PluginInitContext ctx)
    {
        this._context = ctx;
        _context.API.LogDebug(ClassName, "Adding clipboard listener");
        this._clipboard.ClipboardChanged += _OnClipboardChange;

        ClipDir = new DirectoryInfo(ctx.CurrentPluginMetadata.PluginDirectory);
        var imageCacheDirectoryPath = Path.Combine(ClipDir.FullName, "CachedImages");
        ClipCacheDir = !Directory.Exists(imageCacheDirectoryPath)
            ? Directory.CreateDirectory(imageCacheDirectoryPath)
            : new DirectoryInfo(imageCacheDirectoryPath);

        _defaultIconPath = Path.Join(ClipDir.FullName, "Images/clipboard.png");
        _defaultPinIconPath = Path.Join(ClipDir.FullName, "Images/clipboard_pined.png");

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
        _context.API.LogDebug(ClassName, "Created settings successfully");

        _maxDataCount = _settings.MaxDataCount;
        // restore records
        _context.API.LogInfo(ClassName, $"{_settings}");
        _dbPath = Path.Join(ClipDir.FullName, _settings.DbPath);
        _context.API.LogDebug(ClassName, $"Using database at: {_dbPath}");
        _dbHelper = new DbHelper(_dbPath);
        RestoreRecordsFromDb();
    }

    private async void RestoreRecordsFromDb()
    {
        if (!File.Exists(_dbPath))
        {
            _dbHelper.CreateDb();
            return;
        }

        var records = await _dbHelper.GetAllRecord();
        if (records.Count > 0)
        {
            _dataList = records;
            CurrentScore = records.Max(r => r.Score);
        }

        _context.API.LogWarn(ClassName, "Restore records successfully");
    }

    public List<Result> Query(Query query)
    {
        var results = new List<Result>();
        if (query.FirstSearch == _settings.ClearKeyword)
        {
            results.AddRange(
                new[]
                {
                    new Result
                    {
                        Title = "records",
                        SubTitle = "clear records in memory only",
                        // IcoPath = _defaultIconPath,
                        Icon = () =>
                            MaterialIconKind.Close.ToBitmapImage(fillColor: _settings.IconColor),
                        Score = 21,
                        Action = _ =>
                        {
                            _dataList.Clear();
                            return true;
                        },
                    },
                    new Result
                    {
                        Title = "database",
                        SubTitle = "clear records in database too",
                        Icon = () =>
                            MaterialIconKind.DatabaseAlert.ToBitmapImage(
                                fillColor: _settings.IconColor
                            ),
                        Score = 1,
                        Action = _ =>
                        {
                            _dataList.Clear();
                            _dbHelper.DeleteAllRecords();
                            CurrentScore = 1;
                            return true;
                        },
                    }
                }
            );

            return results;
        }

        var displayData =
            query.Search.Trim().Length == 0
                ? _dataList.ToArray()
                : _dataList
                    .Where(
                        i =>
                            !string.IsNullOrEmpty(i.Text)
                            && i.Text.ToLower().Contains(query.Search.Trim().ToLower())
                    )
                    .ToArray();

        results.AddRange(displayData.Select(ClipDataToResult));
        _context.API.LogDebug(ClassName, "Added to result");
        results.Add(
            new Result
            {
                Title = "Clear All Records",
                SubTitle = "Click to clear all records",
                IcoPath = _defaultIconPath,
                Score = 1,
                Action = _ =>
                {
                    _context.API.ChangeQuery(
                        _context.CurrentPluginMetadata.ActionKeyword + " clear ",
                        true
                    );
                    return false;
                },
            }
        );
        return results;
    }

    private Result ClipDataToResult(ClipboardData o)
    {
        var dispSubTitle = $"{o.CreateTime:yyyy-MM-dd-hh-mm-ss}: {o.SenderApp}";
        dispSubTitle = o.Pined ? $"{PinUnicode}{dispSubTitle}" : dispSubTitle;
        return new Result
        {
            Title = o.DisplayTitle,
            SubTitle = dispSubTitle,
            Icon = () => o.Icon,
            CopyText = o.Text,
            Score = GetNewScoreByOrderBy(o),
            TitleToolTip = o.Text,
            SubTitleToolTip = dispSubTitle,
            PreviewPanel = new Lazy<UserControl>(
                () =>
                    new PreviewPanel(
                        o,
                        _context,
                        ClipCacheDir,
                        delAction: RemoveFromDatalist,
                        copyAction: CopyToClipboard,
                        pinAction: PinOneRecord
                    )
            ),
            AsyncAction = async _ =>
            {
                CopyToClipboard(o);
                _context.API.HideMainWindow();
                while (_context.API.IsMainWindowVisible())
                    await Task.Delay(100);
                new InputSimulator().Keyboard.ModifiedKeyStroke(
                    VirtualKeyCode.CONTROL,
                    VirtualKeyCode.VK_V
                );
                _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
                return true;
            },
        };
    }

    private void _OnClipboardChange(object? sender, CbMonitor.ClipboardChangedEventArgs e)
    {
        _context.API.LogDebug(ClassName, "Clipboard changed");
        if (e.Content is null)
            return;

        var now = DateTime.Now;
        var clipboardData = new ClipboardData
        {
            HashId = Utils.GetGuid(),
            Text = "",
            DisplayTitle = "",
            Type = e.ContentType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = CurrentScore + 1,
            InitScore = CurrentScore + 1,
            Time = now,
            Pined = false,
            CreateTime = now,
        };

        switch (e.ContentType)
        {
            case CbContentType.Text:
                clipboardData.Text = _clipboard.ClipboardText;
                clipboardData.Icon = MaterialIconKind.Text.ToBitmapImage(
                    fillColor: _settings.IconColor
                );
                _context.API.LogDebug(ClassName, "Processed text change");
                break;
            case CbContentType.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (_settings.CacheImages)
                    Utils.SaveImageCache(clipboardData, ClipCacheDir);
                var img = _clipboard.ClipboardImage;
                if (img != null)
                    clipboardData.Icon = img.ToBitmapImage();
                _context.API.LogDebug(ClassName, "Processed image change");
                break;
            case CbContentType.Files:
                var t = _clipboard.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                clipboardData.Icon = MaterialIconKind.FileMultiple.ToBitmapImage(
                    _settings.IconColor
                );
                _context.API.LogDebug(ClassName, "Processed file change");
                break;
            case CbContentType.Other:
                // TODO: nothing to do now
                _context.API.LogDebug(ClassName, "Other change listened, skip");
                return;
            default:
                break;
        }

        clipboardData.DisplayTitle = Regex.Replace(clipboardData.Text.Trim(), @"(\r|\n|\t|\v)", "");

        // make sure no repeat
        if (_dataList.Any(node => node.GetMd5() == clipboardData.GetMd5()))
            return;
        _context.API.LogDebug(ClassName, "Adding to dataList");
        _dataList.AddFirst(clipboardData);
        _context.API.LogDebug(ClassName, "Adding to database");
        var isAdd =
            (_settings.KeepText && clipboardData.Type == CbContentType.Text)
            || (_settings.KeepImage && clipboardData.Type == CbContentType.Image)
            || (_settings.KeepFile && clipboardData.Type == CbContentType.Files);
        if (isAdd)
            _dbHelper.AddOneRecord(clipboardData);
        if (_dataList.Count > _maxDataCount)
            _dataList.RemoveLast();
        CurrentScore++;
        _context.API.LogDebug(ClassName, "Processing clipboard change finished");
    }

    public Control CreateSettingPanel()
    {
        _context.API.LogWarn(ClassName, $"{_settings}");
        return new SettingsPanel(_settings, _context);
    }

    public void Dispose()
    {
        try
        {
            // Delete expired records
            var kv = new List<Tuple<CbContentType, int>>
            {
                new(CbContentType.Text, _settings.KeepTextHours),
                new(CbContentType.Image, _settings.KeepImageHours),
                new(CbContentType.Files, _settings.KeepFileHours),
            };
            foreach (var pair in kv)
            {
                _dbHelper?.DeleteRecordByKeepTime(
                    (int)pair.Item1,
                    CmBoxIndexMapper.ToKeepTime(pair.Item2)
                );
            }

            _dbHelper?.Close();
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public void CopyToClipboard(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
        _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void RemoveFromDatalist(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        _dbHelper.DeleteOneRecord(clipboardData);
        _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void PinOneRecord(ClipboardData c)
    {
        _dataList.Remove(c);
        if (c.Type is CbContentType.Text or CbContentType.Files)
            c.Icon = c.Pined
                ? new BitmapImage(new Uri(_defaultPinIconPath, UriKind.RelativeOrAbsolute))
                : new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute));
        _dataList.AddLast(c);
        _context.API.ShowMsg($"{c.Pined}, hash: {c.HashId}");
        _dbHelper.PinOneRecord(c);
        _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public int GetNewScoreByOrderBy(ClipboardData clipboardData)
    {
        if (clipboardData.Pined)
            return int.MaxValue;
        var orderBy = (CbOrders)_settings.OrderBy;
        int score = 0;
        switch (orderBy)
        {
            case CbOrders.Score:
                score = clipboardData.Score;
                break;
            case CbOrders.CreateTime:
                var ctime = new DateTimeOffset(clipboardData.CreateTime);
                score = Convert.ToInt32(ctime.ToUnixTimeSeconds().ToString()[^9..]);
                break;
            case CbOrders.Type:
                score = (int)clipboardData.Type;
                break;
            case CbOrders.SourceApplication:
                var last = int.Min(clipboardData.SenderApp.Length, 10);
                score = Encoding.UTF8.GetBytes(clipboardData.SenderApp[..last]).Sum(i => i);
                break;
            default:
                break;
        }

        return score;
    }

    public void Save()
    {
        _settings.Save();
    }
}
