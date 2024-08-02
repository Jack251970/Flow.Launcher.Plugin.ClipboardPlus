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
using System.Globalization;
using WindowsInput;

namespace ClipboardPlus;

public partial class ClipboardPlus : IAsyncPlugin, IAsyncReloadable, IContextMenu/*, IPluginI18n*/,
    ISavable, ISettingProvider, IDisposable
{
    #region Properties

    // plugin context
    private PluginInitContext Context = null!;

    // class name for logging
    private string ClassName => GetType().Name;

    // action keyword
    // TODO: Change it but clear action won't change.
    private string ActionKeyword => Context.CurrentPluginMetadata.ActionKeyword ?? "cbp";

    // pinned symbol
    private const string PinUnicode = "📌";

    // settings
    private Settings Settings = null!;

    // database helper
    private DbHelpers DbHelpers = null!;

    // clipboard listener instance
    private CbMonitor ClipboardMonitor = null!;

    // records list
    private LinkedList<ClipboardData> RecordsList = new();
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
        if (query.FirstSearch == Settings.ClearKeyword)
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
                            RecordsList.Clear();
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
                            RecordsList.Clear();
                            await DbHelpers.DeleteAllRecordsAsync();
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
                ? RecordsList.ToArray()
                : RecordsList
                    .Where(
                        i =>
                            !string.IsNullOrEmpty(i.Text)
                            && i.Text.ToLower().Contains(query.Search.Trim().ToLower())
                    )
                    .ToArray();
            results.AddRange(displayData.Select(GetResultFromClipboardData));
            Context.API.LogDebug(ClassName, "Added records successfully");
            // clear results
            results.Add(
                new Result
                {
                    Title = "Clear All Records",
                    SubTitle = "Click to clear all records",
                    IcoPath = PathHelpers.ClearIconPath,
                    Score = Settings.MaxDataCount + 1,
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{ActionKeyword} {Settings.ClearKeyword} ", true);
                        return false;
                    },
                }
            );
        }
        return results;
    }

    public async Task InitAsync(PluginInitContext context)
    {
        Context = context;

        // init path helpers
        PathHelpers.Init(context);

        // init settings
        if (File.Exists(PathHelpers.SettingsPath))
        {
            using var fs = File.OpenRead(PathHelpers.SettingsPath);
            Settings = JsonSerializer.Deserialize<Settings>(fs)!;
        }
        else
        {
            Settings = new Settings();
        }
        Settings.Save();
        Context.API.LogDebug(ClassName, "Init settings successfully");
        Context.API.LogInfo(ClassName, $"{Settings}");

        // init database
        DbHelpers = new DbHelpers(PathHelpers.DatabasePath);
        if (!File.Exists(PathHelpers.DatabasePath))
        {
            await DbHelpers.CreateDbAsync();
            return;
        }
        Context.API.LogDebug(ClassName, "Init database successfully");

        // init clipboard listener
        ClipboardMonitor = new() { ObserveLastEntry = false };
        ClipboardMonitor.ClipboardChanged += OnClipboardChange;
        Context.API.LogDebug(ClassName, "Init clipboard listener");

        // init records
        await InitRecordsFromDb();
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

    #region IContextMenu interface

    public List<Result> LoadContextMenus(Result result)
    {
        var results = new List<Result>();
        if (result.ContextData is not ClipboardData clipboardData)
        {
            return results;
        }

        results.AddRange(
            new[]
            {
                new Result
                {
                    Title = "Copy",
                    SubTitle = "Copy this record to clipboard",
                    IcoPath = PathHelpers.CopyIconPath,
                    Score = 4,
                    Action = _ =>
                    {
                        CopyToClipboard(clipboardData);
                        return true;
                    }
                },
                new Result
                {
                    Title = "Delete in list",
                    SubTitle = "Delete this record in list",
                    IcoPath = PathHelpers.DeleteIconPath,
                    Score = 2,
                    Action = _ =>
                    {
                        RemoveFromList(clipboardData, true);
                        return false;
                    }
                },
                new Result
                {
                    Title = "Delete in list and database",
                    SubTitle = "Delete this record in both list and database",
                    IcoPath = PathHelpers.DeleteIconPath,
                    Score = 1,
                    Action = _ =>
                    {
                        RemoveFromListDatabase(clipboardData, true);
                        return false;
                    }
                },
            }
        );
        if (clipboardData.Pinned)
        {
            results.Add(
                new Result
                {
                    Title = "Unpin",
                    SubTitle = "Unpin this record",
                    IcoPath = PathHelpers.UnpinIconPath,
                    Score = 3,
                    Action = _ =>
                    {
                        PinOneRecord(clipboardData, true);
                        return false;
                    }
                }
            );
        }
        else
        {
            results.Add(
                new Result
                {
                    Title = "Pin",
                    SubTitle = "Pin this record",
                    IcoPath = PathHelpers.PinIconPath,
                    Score = 3,
                    Action = _ =>
                    {
                        PinOneRecord(clipboardData, true);
                        return false;
                    }
                }
            );
        }
        return results;
    }

    #endregion

    #region IPluginI18n interface

    // TODO

    public string GetTranslatedPluginTitle()
    {
        return string.Empty;
    }

    public string GetTranslatedPluginDescription()
    {
        return string.Empty;
    }

    public void OnCultureInfoChanged(CultureInfo newCulture)
    {

    }

    #endregion

    #region ISavable interface

    // Warning: This method will be called after dispose.
    public void Save()
    {
        Settings?.Save();
    }

    #endregion

    #region ISettingProvider interface

    public Control CreateSettingPanel()
    {
        Context.API.LogWarn(ClassName, $"{Settings}");
        return new SettingsPanel(Settings, Context);
    }

    #endregion

    #region Clipboard Monitor

    private async void OnClipboardChange(object? sender, CbMonitor.ClipboardChangedEventArgs e)
    {
        Context.API.LogDebug(ClassName, "Clipboard changed");
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
                clipboardData.Text = ClipboardMonitor.ClipboardText;
                Context.API.LogDebug(ClassName, "Processed text change");
                break;
            case CbContentType.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (Settings.CacheImages)
                {
                    var imageName = StringUtils.FormatImageName(Settings.ImageFormat, clipboardData.CreateTime,
                        clipboardData.SenderApp ?? "");
                    FileUtils.SaveImageCache(clipboardData, PathHelpers.ImageCachePath, imageName);
                }
                var img = ClipboardMonitor.ClipboardImage;
                // TODO: Optimize?
                if (img != null)
                {
                    clipboardData.Icon = img.ToBitmapImage();
                }
                Context.API.LogDebug(ClassName, "Processed image change");
                break;
            case CbContentType.Files:
                var t = ClipboardMonitor.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                Context.API.LogDebug(ClassName, "Processed file change");
                break;
            case CbContentType.Other:
                // TODO: Handle other formats.
                Context.API.LogDebug(ClassName, "Other change listened, skip");
                return;
            default:
                break;
        }
        clipboardData.Icon = GetDefaultIcon(clipboardData);
        clipboardData.DisplayTitle = MyRegex().Replace(clipboardData.Text.Trim(), "");

        // add to list and database if no repeat 
        if (RecordsList.Any(node => node.GetMd5() == clipboardData.GetMd5()))
        {
            return;
        }
        RecordsList.AddFirst(clipboardData);
        Context.API.LogDebug(ClassName, "Added to list");

        // add to database if needed
        var needAddDatabase =
            Settings.KeepText && clipboardData.Type == CbContentType.Text
            || Settings.KeepImage && clipboardData.Type == CbContentType.Image
            || Settings.KeepFile && clipboardData.Type == CbContentType.Files;
        if (needAddDatabase)
        {
            await DbHelpers.AddOneRecordAsync(clipboardData);
        }
        Context.API.LogDebug(ClassName, "Added to database");

        // remove last record if needed
        if (RecordsList.Count >= Settings.MaxDataCount)
        {
            RecordsList.RemoveLast();
        }
        Context.API.LogDebug(ClassName, "Processing clipboard change finished");
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
            foreach (var pair in Settings.KeepTimePairs)
            {
                Context.API.LogInfo(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                await DbHelpers.DeleteRecordByKeepTimeAsync(
                    (int)pair.Item1,
                    pair.Item2.ToKeepTime()
                );
            }
            Context.API.LogWarn(ClassName, $"Cleared expired records successfully");
        }
        catch (Exception e)
        {
            Context.API.LogWarn(ClassName, $"Cleared expired records failed\n{e}");
        }

        // restore records
        var records = await DbHelpers.GetAllRecordAsync();
        if (records.Count > 0)
        {
            RecordsList = records;
            CurrentScore = records.Max(r => r.Score);
        }
        Context.API.LogWarn(ClassName, "Restored records successfully");
    }

    #endregion

    #region Query Result

    private Result GetResultFromClipboardData(ClipboardData clipboardData)
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
            ContextData = clipboardData,
            PreviewPanel = new Lazy<UserControl>(
                () =>
                    new PreviewPanel(
                        clipboardData,
                        Context,
                        copyAction: CopyToClipboard,
                        pinAction: (d) => PinOneRecord(d, false),
                        delAction: (d) => RemoveFromListDatabase(d, false)
                    )
            ),
            AsyncAction = async _ =>
            {
                switch (Settings.ClickAction)
                {
                    case ClickAction.CopyPaste:
                        CopyToClipboard(clipboardData);
                        Context.API.HideMainWindow();
                        while (Context.API.IsMainWindowVisible())
                        {
                            await Task.Delay(100);
                        }
                        new InputSimulator().Keyboard.ModifiedKeyStroke(
                            VirtualKeyCode.CONTROL,
                            VirtualKeyCode.VK_V
                        );
                        break;
                    case ClickAction.CopyDeleteList:
                        CopyToClipboard(clipboardData);
                        RemoveFromList(clipboardData, false);
                        break;
                    case ClickAction.CopyDeleteListDatabase:
                        CopyToClipboard(clipboardData);
                        RemoveFromListDatabase(clipboardData, false);
                        break;
                    case ClickAction.CopyPasteDeleteList:
                        CopyToClipboard(clipboardData);
                        Context.API.HideMainWindow();
                        while (Context.API.IsMainWindowVisible())
                        {
                            await Task.Delay(100);
                        }
                        new InputSimulator().Keyboard.ModifiedKeyStroke(
                            VirtualKeyCode.CONTROL,
                            VirtualKeyCode.VK_V
                        );
                        RemoveFromList(clipboardData, false);
                        break;
                    case ClickAction.CopyPasteDeleteListDatabase:
                        CopyToClipboard(clipboardData);
                        Context.API.HideMainWindow();
                        while (Context.API.IsMainWindowVisible())
                        {
                            await Task.Delay(100);
                        }
                        new InputSimulator().Keyboard.ModifiedKeyStroke(
                            VirtualKeyCode.CONTROL,
                            VirtualKeyCode.VK_V
                        );
                        RemoveFromListDatabase(clipboardData, false);
                        break;
                    default:
                        CopyToClipboard(clipboardData);
                        break;
                }
                return true;
            },
        };
    }

    private int GetNewScoreByOrderBy(ClipboardData clipboardData)
    {
        if (clipboardData.Pinned)
        {
            return int.MaxValue;
        }

        var orderBy = Settings.OrderBy;
        int score = 0;
        switch (orderBy)
        {
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
                score = clipboardData.Score;
                break;
        }

        return score;
    }

    #endregion

    #region Clipboard Actions

    private void CopyToClipboard(ClipboardData clipboardData)
    {
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
    }

    private void RemoveFromList(ClipboardData clipboardData, bool needEsc = false)
    {
        RecordsList.Remove(clipboardData);
        if (needEsc)
        {
            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        }
        Context.API.ReQuery(false);
    }

    private async void RemoveFromListDatabase(ClipboardData clipboardData, bool needEsc = false)
    {
        RecordsList.Remove(clipboardData);
        await DbHelpers.DeleteOneRecordAsync(clipboardData);
        if (needEsc)
        {
            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        }
        Context.API.ReQuery(false);
    }

    private async void PinOneRecord(ClipboardData clipboardData, bool needEsc = false)
    {
        clipboardData.Pinned = !clipboardData.Pinned;
        clipboardData.Score = clipboardData.Pinned ? int.MaxValue : clipboardData.InitScore;
        RecordsList.Remove(clipboardData);
        RecordsList.AddLast(clipboardData);
        await DbHelpers.PinOneRecordAsync(clipboardData);
        // TODO: Ask Flow-Launcher for a better way to refresh the query.
        if (needEsc)
        {
            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        }
        Context.API.ReQuery(false);
    }

    #endregion

    #region Icons Recources

    private static readonly BitmapImage AppIcon = new(new Uri(PathHelpers.AppIconPath, UriKind.RelativeOrAbsolute));
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

    #region IDisposable Interface

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Context.API.LogWarn(ClassName, $"Enter dispose");
            if (ClipboardMonitor != null)
            {
                ClipboardMonitor.ClipboardChanged -= OnClipboardChange;
                ClipboardMonitor.Dispose();
                ClipboardMonitor = null!;
                Context.API.LogWarn(ClassName, $"Disposed ClipboardMonitor");
            }
            if (DbHelpers != null)
            {
                DbHelpers?.Dispose();
                DbHelpers = null!;
                Context.API.LogWarn(ClassName, $"Disposed DbHelpers");
            }
            Settings = null!;
            RecordsList = null!;
        }
    }

    #endregion
}
