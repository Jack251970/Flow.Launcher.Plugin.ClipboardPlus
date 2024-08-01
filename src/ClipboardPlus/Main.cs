using ClipboardPlus.Core;
using ClipboardPlus.Panels;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ClipboardPlus;

public partial class ClipboardPlus : IAsyncPlugin, ISettingProvider, ISavable, IAsyncReloadable, IDisposable
{
    #region Properties

    // plugin context
    private PluginInitContext _context = null!;

    // class name for logging
    private string ClassName => GetType().Name;

    // action keyword
    private string ActionKeyword => _context.CurrentPluginMetadata.ActionKeyword ?? "cbp";

    // pinned symbol
    private const string PinUnicode = "📌";

    // settings
    private Settings _settings = null!;

    // database helper
    private DbHelpers _dbHelper = null!;

    // clipboard listener instance
    private readonly CbMonitor _clipboard = new() { ObserveLastEntry = false };

    // records list
    private LinkedList<ClipboardData> _recordsList = new();
    private int CurrentScore = 1;

    #endregion

    #region IAsyncPlugin interface

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        return Task.Run(() => Query(query));
    }

    // TODO: Support Glphy in the result.
    public List<Result> Query(Query query)
    {
        var results = new List<Result>();
        if (query.FirstSearch == _settings.ClearKeyword)
        {
            // clear actions results
            results.AddRange(
                new[]
                {
                    new Result
                    {
                        Title = "Clear list",
                        SubTitle = "Clear records in list",
                        IcoPath = PathHelpers.ListIconPath,
                        Score = 2,
                        Action = _ =>
                        {
                            _recordsList.Clear();
                            return true;
                        },
                    },
                    new Result
                    {
                        Title = "Clear all",
                        SubTitle = "Clear records in both list and database",
                        IcoPath = PathHelpers.DatabaseIconPath,
                        Score = 1,
                        AsyncAction = async _ =>
                        {
                            _recordsList.Clear();
                            await _dbHelper.DeleteAllRecordsAsync();
                            CurrentScore = 1;
                            return true;
                        }
                    }
                }
            );
        }
        else
        {
            // records results
            var displayData =
            query.Search.Trim().Length == 0
                ? _recordsList.ToArray()
                : _recordsList
                    .Where(
                        i =>
                            !string.IsNullOrEmpty(i.Text)
                            && i.Text.ToLower().Contains(query.Search.Trim().ToLower())
                    )
                    .ToArray();
            results.AddRange(displayData.Select(ClipDataToResult));
            _context.API.LogDebug(ClassName, "Added records successfully");
            // clear results
            results.Add(
                new Result
                {
                    Title = "Clear All Records",
                    SubTitle = "Click to clear all records",
                    IcoPath = PathHelpers.ClearIconPath,
                    Score = _settings.MaxDataCount + 1,
                    Action = _ =>
                    {
                        _context.API.ChangeQuery(ActionKeyword + " clear ", true);
                        return false;
                    },
                }
            );
        }
        return results;
    }

    public async Task InitAsync(PluginInitContext context)
    {
        _context = context;

        // init path helpers
        PathHelpers.Init(context);

        // init settings
        if (File.Exists(PathHelpers.SettingsPath))
        {
            using var fs = File.OpenRead(PathHelpers.SettingsPath);
            _settings = JsonSerializer.Deserialize<Settings>(fs)!;
        }
        else
        {
            _settings = new Settings();
        }
        _settings.Save();
        _context.API.LogDebug(ClassName, "Init settings successfully");
        _context.API.LogInfo(ClassName, $"{_settings}");

        // init database
        _dbHelper = new DbHelpers(PathHelpers.DatabasePath);
        if (!File.Exists(PathHelpers.DatabasePath))
        {
            await _dbHelper.CreateDbAsync();
            return;
        }
        _context.API.LogDebug(ClassName, "Init database successfully");

        // init clipboard listener
        _clipboard.ClipboardChanged += OnClipboardChange;
        _context.API.LogDebug(ClassName, "Init clipboard listener");

        // init records
        await InitRecordsFromDb();
    }

    #endregion

    #region ISettingProvider interface

    public Control CreateSettingPanel()
    {
        _context.API.LogWarn(ClassName, $"{_settings}");
        return new SettingsPanel(_settings, _context);
    }

    #endregion

    #region ISavable interface

    public void Save()
    {
        _settings.Save();
    }

    #endregion

    #region IAsyncReloadable interface

    public async Task ReloadDataAsync()
    {
        // save settings
        Save();

        // init records
        await InitRecordsFromDb();
    }

    #endregion

    #region Clipboard Monitor

    private async void OnClipboardChange(object? sender, CbMonitor.ClipboardChangedEventArgs e)
    {
        _context.API.LogDebug(ClassName, "Clipboard changed");
        if (e.Content is null)
        {
            return;
        }

        // init clipboard data
        var now = DateTime.Now;
        var clipboardData = new ClipboardData
        {
            HashId = StringUtils.GetGuid(),
            Text = "",
            DisplayTitle = "",
            Type = e.ContentType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            IconPath = PathHelpers.AppIconPath,
            Icon = AppIcon,
            PreviewImagePath = PathHelpers.AppIconPath,
            Score = CurrentScore + 1,
            InitScore = CurrentScore + 1,
            Time = now,
            Pinned = false,
            CreateTime = now,
        };
        CurrentScore++;

        // process clipboard data
        switch (e.ContentType)
        {
            case CbContentType.Text:
                clipboardData.Text = _clipboard.ClipboardText;
                _context.API.LogDebug(ClassName, "Processed text change");
                break;
            case CbContentType.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (_settings.CacheImages)
                {
                    var imageName = StringUtils.FormatImageName(_settings.ImageFormat, clipboardData.CreateTime,
                        clipboardData.SenderApp ?? "");
                    FileUtils.SaveImageCache(clipboardData, PathHelpers.ImageCachePath, imageName);
                }
                var img = _clipboard.ClipboardImage;
                // TODO: Optimize?
                if (img != null)
                {
                    clipboardData.Icon = img.ToBitmapImage();
                }
                _context.API.LogDebug(ClassName, "Processed image change");
                break;
            case CbContentType.Files:
                var t = _clipboard.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                _context.API.LogDebug(ClassName, "Processed file change");
                break;
            case CbContentType.Other:
                // TODO: Handle other formats.
                _context.API.LogDebug(ClassName, "Other change listened, skip");
                return;
            default:
                break;
        }
        clipboardData.Icon = GetDefaultIcon(clipboardData);
        clipboardData.DisplayTitle = MyRegex().Replace(clipboardData.Text.Trim(), "");

        // add to list and database if no repeat 
        if (_recordsList.Any(node => node.GetMd5() == clipboardData.GetMd5()))
        {
            return;
        }
        _recordsList.AddFirst(clipboardData);
        _context.API.LogDebug(ClassName, "Added to list");

        // add to database if needed
        var needAddDatabase = 
            _settings.KeepText && clipboardData.Type == CbContentType.Text
            || _settings.KeepImage && clipboardData.Type == CbContentType.Image
            || _settings.KeepFile && clipboardData.Type == CbContentType.Files;
        if (needAddDatabase)
        {
            await _dbHelper.AddOneRecordAsync(clipboardData);
        }
        _context.API.LogDebug(ClassName, "Added to database");

        // remove last record if needed
        if (_recordsList.Count >= _settings.MaxDataCount)
        {
            _recordsList.RemoveLast();
        }
        _context.API.LogDebug(ClassName, "Processing clipboard change finished");
    }

    [GeneratedRegex("(\\r|\\n|\\t|\\v)")]
    private static partial Regex MyRegex();

    #endregion

    #region Database Functions

    private async Task InitRecordsFromDb()
    {
        // clear expired records
        try
        {
            foreach (var pair in _settings.KeepTimePairs)
            {
                _context.API.LogInfo(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                await _dbHelper.DeleteRecordByKeepTimeAsync(
                    (int)pair.Item1,
                    pair.Item2.ToKeepTime()
                );
            }
            _context.API.LogWarn(ClassName, $"Cleared expired records successfully");
        }
        catch (Exception e)
        {
            _context.API.LogWarn(ClassName, $"Cleared expired records failed\n{e}");
        }

        // restore records
        var records = await _dbHelper.GetAllRecordAsync();
        if (records.Count > 0)
        {
            _recordsList = records;
            CurrentScore = records.Max(r => r.Score);
        }
        _context.API.LogWarn(ClassName, "Restored records successfully");
    }

    #endregion

    #region Clipboard Result

    private Result ClipDataToResult(ClipboardData clipboardData)
    {
        var dispSubTitle = $"{clipboardData.CreateTime:yyyy-MM-dd-hh-mm-ss}: {clipboardData.SenderApp}";
        dispSubTitle = clipboardData.Pinned ? $"{PinUnicode}{dispSubTitle}" : dispSubTitle;
        return new Result
        {
            Title = clipboardData.DisplayTitle,
            SubTitle = dispSubTitle,
            Icon = () => clipboardData.Icon,
            CopyText = clipboardData.Text,
            Score = GetNewScoreByOrderBy(clipboardData),
            TitleToolTip = clipboardData.Text,
            SubTitleToolTip = dispSubTitle,
            PreviewPanel = new Lazy<UserControl>(
                () =>
                    new PreviewPanel(
                        clipboardData,
                        _context,
                        delAction: RemoveFromDatalist,
                        copyAction: CopyToClipboard,
                        pinAction: PinOneRecord
                    )
            ),
            AsyncAction = async _ =>
            {
                CopyToClipboard(clipboardData);
                _context.API.HideMainWindow();
                while (_context.API.IsMainWindowVisible())
                {
                    await Task.Delay(100);
                }
                _context.API.ChangeQuery(ActionKeyword, true);
                return true;
            },
        };
    }

    #endregion

    #region Clipboard Actions

    private void CopyToClipboard(ClipboardData clipboardData)
    {
        _recordsList.Remove(clipboardData);
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
        _context.API.ChangeQuery(ActionKeyword, true);
    }

    private async void RemoveFromDatalist(ClipboardData clipboardData)
    {
        _recordsList.Remove(clipboardData);
        await _dbHelper.DeleteOneRecordAsync(clipboardData);
        _context.API.ChangeQuery(ActionKeyword, true);
    }

    private async void PinOneRecord(ClipboardData clipboardData)
    {
        _recordsList.Remove(clipboardData);
        if (clipboardData.Type is CbContentType.Text or CbContentType.Files)
        {
            clipboardData.Icon = clipboardData.Pinned ? PinnedIcon : GetDefaultIcon(clipboardData);
        }

        _recordsList.AddLast(clipboardData);
        await _dbHelper.PinOneRecordAsync(clipboardData);
        _context.API.ChangeQuery(ActionKeyword, true);
    }

    private int GetNewScoreByOrderBy(ClipboardData clipboardData)
    {
        if (clipboardData.Pinned)
        {
            return int.MaxValue;
        }

        var orderBy = _settings.OrderBy;
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

    #endregion

    #region Icons Recources

    private static readonly BitmapImage AppIcon = new(new Uri(PathHelpers.AppIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage PinnedIcon = new(new Uri(PathHelpers.PinnedIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage TextIcon = new(new Uri(PathHelpers.TextIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage FilesIcon = new(new Uri(PathHelpers.FileIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage ImageIcon = new(new Uri(PathHelpers.ImageIconPath, UriKind.RelativeOrAbsolute));

    private static BitmapImage GetDefaultIcon(ClipboardData data)
    {
        return data.Type switch
        {
            CbContentType.Text => TextIcon,
            CbContentType.Files => FilesIcon,
            CbContentType.Image => ImageIcon,
            _ => AppIcon
        };
    }

    #endregion

    #region IDisposable interface

    public async void Dispose()
    {
        _context.API.LogWarn(ClassName, $"enter dispose");
        await ReloadDataAsync();
        _clipboard.Dispose();
        _dbHelper.Dispose();
    }

    #endregion
}
