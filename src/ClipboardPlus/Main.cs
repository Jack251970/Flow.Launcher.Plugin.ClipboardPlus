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
using WindowsInput;
using System.Globalization;

namespace ClipboardPlus;

public partial class ClipboardPlus : IAsyncPlugin, IAsyncReloadable, IContextMenu, IPluginI18n,
    ISavable, ISettingProvider, IDisposable
{
    #region Properties

    // Plugin context
    private PluginInitContext Context = null!;

    // Class name for logging
    private string ClassName => GetType().Name;

    // Action keyword
    // TODO: Change it but clear action won't change.
    private string ActionKeyword => Context.CurrentPluginMetadata.ActionKeyword ?? "cbp";

    // Pinned symbol
    private const string PinUnicode = "📌";

    // Settings
    private Settings Settings = null!;

    // Settings view model
    private SettingsViewModel SettingsViewModel = null!;

    // Database helper
    private DbHelper DbHelper = null!;

    // Clipboard monitor instance
    // Warning: Do not init the instance in InitAsync function! This will cause issues.
    private CbMonitor ClipboardMonitor = new() { ObserveLastEntry = false };

    // Records list & Score
    private LinkedList<ClipboardData> RecordsList = new();
    private int CurrentScore = 1;

    #endregion

    #region IAsyncPlugin Interface

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
                        Title = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_title"),
                        SubTitle = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_subtitle"),
                        IcoPath = PathHelper.ListIconPath,
                        Glyph = ResourceHelper.ListGlyph,
                        Score = 2,
                        Action = _ =>
                        {
                            RecordsList.Clear();
                            return true;
                        },
                    },
                    new Result
                    {
                        Title = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_title"),
                        SubTitle = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_subtitle"),
                        IcoPath = PathHelper.DatabaseIconPath,
                        Glyph = ResourceHelper.DatabaseGlyph,
                        Score = 1,
                        AsyncAction = async _ =>
                        {
                            RecordsList.Clear();
                            await DbHelper.DeleteAllRecordsAsync();
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
                    Title = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_title"),
                    SubTitle = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_subtitle"),
                    IcoPath = PathHelper.ClearIconPath,
                    Glyph = ResourceHelper.ClearGlyph,
                    Score = Settings.MaxRecords + 1,
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
        PathHelper.Init(context);

        // init settings
        if (File.Exists(PathHelper.SettingsPath))
        {
            using var fs = File.OpenRead(PathHelper.SettingsPath);
            Settings = JsonSerializer.Deserialize<Settings>(fs)!;
        }
        else
        {
            Settings = new Settings();
        }
        Settings.Save();
        Context.API.LogDebug(ClassName, "Init settings successfully");
        Context.API.LogInfo(ClassName, $"{Settings}");

        // init settings viewmodel
        SettingsViewModel = new SettingsViewModel(context, Settings, ReloadDataAsync, Save);

        // init database & records
        DbHelper = new DbHelper(PathHelper.DatabasePath);
        if (File.Exists(PathHelper.DatabasePath))
        {
            await InitRecordsFromDb();
        }
        else
        {
            await DbHelper.CreateDbAsync();
        }
        Context.API.LogDebug(ClassName, "Init database successfully");

        // init clipboard monitor
        ClipboardMonitor.ClipboardChanged += OnClipboardChange;
        Context.API.LogDebug(ClassName, "Init clipboard monitor successfully");
    }

    #endregion

    #region IAsyncReloadable Interface

    public async Task ReloadDataAsync()
    {
        // reload records
        await InitRecordsFromDb();
    }

    #endregion

    #region IContextMenu Interface

    public List<Result> LoadContextMenus(Result result)
    {
        var results = new List<Result>();
        if (result.ContextData is not ClipboardData clipboardData)
        {
            return results;
        }

        var pinned = clipboardData.Pinned;
        var pinStr = pinned ? "unpin" : "pin";
        results.AddRange(
            new[]
            {
                new Result
                {
                    Title = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_copy_title"),
                    SubTitle = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_copy_subtitle"),
                    IcoPath = PathHelper.CopyIconPath,
                    Glyph = ResourceHelper.CopyGlyph,
                    Score = 3,
                    Action = _ =>
                    {
                        CopyToClipboard(clipboardData);
                        return true;
                    }
                },
                new Result
                {
                    Title = Context.API.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_title"),
                    SubTitle = Context.API.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_subtitle"),
                    IcoPath = PathHelper.GetPinIconPath(pinned),
                    Glyph = ResourceHelper.GetPinGlyph(pinned),
                    Score = 2,
                    Action = _ =>
                    {
                        PinOneRecord(clipboardData, true);
                        return false;
                    }
                },
                new Result
                {
                    Title = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_delete_title"),
                    SubTitle = Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_delete_subtitle"),
                    IcoPath = PathHelper.DeleteIconPath,
                    Glyph = ResourceHelper.DeleteGlyph,
                    Score = 1,
                    Action = _ =>
                    {
                        RemoveFromListDatabase(clipboardData, true);
                        return false;
                    }
                },
            }
        );
        return results;
    }

    #endregion

    #region IPluginI18n Interface

    public string GetTranslatedPluginTitle()
    {
        return Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_name");
    }

    public string GetTranslatedPluginDescription()
    {
        return Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_description");
    }

    public void OnCultureInfoChanged(CultureInfo newCulture)
    {
        SettingsViewModel.OnCultureInfoChanged(newCulture);
    }

    #endregion

    #region ISavable Interface

    // Warning: This method will be called after dispose.
    public void Save()
    {
        Settings?.Save();
    }

    #endregion

    #region ISettingProvider Interface

    public Control CreateSettingPanel()
    {
        Context.API.LogWarn(ClassName, $"{Settings}");
        return new SettingsPanel(SettingsViewModel);
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
            IconPath = PathHelper.AppIconPath,
            Icon = null!,
            Glyph = null!,
            PreviewImagePath = PathHelper.AppIconPath,
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
            case DataType.Text:
                clipboardData.Text = ClipboardMonitor.ClipboardText;
                Context.API.LogDebug(ClassName, "Processed text change");
                break;
            case DataType.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (Settings.CacheImages)
                {
                    var imageName = StringUtils.FormatImageName(Settings.CacheFormat, clipboardData.CreateTime,
                        clipboardData.SenderApp ?? Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_unknown_app"));
                    FileUtils.SaveImageCache(clipboardData, PathHelper.ImageCachePath, imageName);
                }
                var img = ClipboardMonitor.ClipboardImage;
                // TODO: Optimize?
                if (img != null)
                {
                    clipboardData.Icon = img.ToBitmapImage();
                }
                Context.API.LogDebug(ClassName, "Processed image change");
                break;
            case DataType.Files:
                var t = ClipboardMonitor.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                Context.API.LogDebug(ClassName, "Processed file change");
                break;
            case DataType.Other:
                // TODO: Handle other formats.
                Context.API.LogDebug(ClassName, "Other change listened, skip");
                return;
            default:
                break;
        }
        clipboardData.Icon = ResourceHelper.GetIcon(clipboardData.Type);
        clipboardData.Glyph = ResourceHelper.GetGlyph(clipboardData.Type);
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
            Settings.KeepText && clipboardData.Type == DataType.Text
            || Settings.KeepImages && clipboardData.Type == DataType.Image
            || Settings.KeepFiles && clipboardData.Type == DataType.Files;
        if (needAddDatabase)
        {
            await DbHelper.AddOneRecordAsync(clipboardData);
        }
        Context.API.LogDebug(ClassName, "Added to database");

        // remove last record if needed
        if (RecordsList.Count >= Settings.MaxRecords)
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
                await DbHelper.DeleteRecordByKeepTimeAsync(
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
        var records = await DbHelper.GetAllRecordAsync();
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
        // TODO: Add CultureInfo support.
        var dispSubTitle = $"{clipboardData.CreateTime:yyyy-MM-dd-hh-mm-ss}: {clipboardData.SenderApp}";
        dispSubTitle = clipboardData.Pinned ? $"{PinUnicode}{dispSubTitle}" : dispSubTitle;
        return new Result
        {
            Title = clipboardData.DisplayTitle,
            SubTitle = dispSubTitle,
            Icon = () => clipboardData.Icon,
            Glyph = clipboardData.Glyph,
            CopyText = clipboardData.Text,
            Score = GetNewScoreByOrderBy(clipboardData),
            TitleToolTip = clipboardData.Text,
            SubTitleToolTip = dispSubTitle,
            ContextData = clipboardData,
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(Context, clipboardData)),
            AsyncAction = async _ =>
            {
                // TODO: Fix bug here.
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
            // TODO: Change this under the clear action.
            return int.MaxValue;
        }

        var orderBy = Settings.RecordOrder;
        int score = 0;
        switch (orderBy)
        {
            case RecordOrder.CreateTime:
                var ctime = new DateTimeOffset(clipboardData.CreateTime);
                score = Convert.ToInt32(ctime.ToUnixTimeSeconds().ToString()[^9..]);
                break;
            case RecordOrder.DataType:
                score = (int)clipboardData.Type;
                break;
            case RecordOrder.SourceApplication:
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

    private static void CopyToClipboard(ClipboardData clipboardData)
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
        await DbHelper.DeleteOneRecordAsync(clipboardData);
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
        await DbHelper.PinOneRecordAsync(clipboardData);
        // TODO: Ask Flow-Launcher for a better way to refresh the query.
        if (needEsc)
        {
            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        }
        Context.API.ReQuery(false);
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
            Context.API.LogDebug(ClassName, $"Enter dispose");
            if (DbHelper != null)
            {
                DbHelper?.Dispose();
                DbHelper = null!;
                Context.API.LogDebug(ClassName, $"Disposed DbHelper");
            }
            ClipboardMonitor.ClipboardChanged -= OnClipboardChange;
            ClipboardMonitor.Dispose();
            ClipboardMonitor = null!;
            Context.API.LogDebug(ClassName, $"Disposed ClipboardMonitor");
            SettingsViewModel.Dispose();
            SettingsViewModel = null!;
            Context.API.LogDebug(ClassName, $"Disposed SettingsViewModel");
            Settings = null!;
            RecordsList = null!;
            Context.API.LogDebug(ClassName, $"Disposed Other Components");
        }
    }

    #endregion
}
