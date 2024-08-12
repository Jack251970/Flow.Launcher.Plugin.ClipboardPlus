using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using WindowsInput;

namespace Flow.Launcher.Plugin.ClipboardPlus;

public partial class ClipboardPlus : IAsyncPlugin, IAsyncReloadable, IContextMenu, IPluginI18n,
    ISavable, ISettingProvider, IClipboardPlus, IDisposable
{
    #region Properties

    // Plugin context
    private PluginInitContext Context = null!;

    // Class name for logging
    private string ClassName => GetType().Name;

    // Culture info
    private CultureInfo CultureInfo = null!;

    // Pinned symbol
    private const string PinUnicode = "📌";

    // Settings
    private Settings Settings = null!;

    // Database helper
    private DatabaseHelper DatabaseHelper = null!;

    // Clipboard monitor instance
    // Warning: Do not init the instance in InitAsync function! This will cause issues.
    private ClipboardMonitor ClipboardMonitor = new() { ObserveLastEntry = false };

    // Records list & Score
    private LinkedList<ClipboardData> RecordsList = new();
    private int CurrentScore = 1;

    #endregion

    #region IAsyncPlugin Interface

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        return Task.Run(() => Query(query));
    }

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
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_subtitle"),
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
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_subtitle"),
                        IcoPath = PathHelper.DatabaseIconPath,
                        Glyph = ResourceHelper.DatabaseGlyph,
                        Score = 1,
                        AsyncAction = async _ =>
                        {
                            RecordsList.Clear();
                            await DatabaseHelper.DeleteAllRecordsAsync();
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
            var records = query.Search.Trim().Length == 0
                ? RecordsList.ToArray()
                : RecordsList.Where(i => !string.IsNullOrEmpty(i.Text) && i.Text.ToLower().Contains(query.Search.Trim().ToLower())).ToArray();
            results.AddRange(records.Select(GetResultFromClipboardData));
            Context.API.LogDebug(ClassName, "Added records successfully");
            // clear results
            results.Add(
                new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_subtitle"),
                    IcoPath = PathHelper.ClearIconPath,
                    Glyph = ResourceHelper.ClearGlyph,
                    Score = Settings.MaxRecords + 1,
                    Action = _ =>
                    {
                        Context.API.ChangeQuery($"{query.ActionKeyword} {Settings.ClearKeyword} ", true);
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
        Settings = context.API.LoadSettingJsonStorage<Settings>();
        Context.API.LogDebug(ClassName, "Init settings successfully");
        Context.API.LogInfo(ClassName, $"{Settings}");

        // init database & records
        DatabaseHelper = new DatabaseHelper(PathHelper.DatabasePath);
        if (File.Exists(PathHelper.DatabasePath))
        {
            await InitRecordsFromDatabase();
        }
        else
        {
            await DatabaseHelper.CreateDatabaseAsync();
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
        await InitRecordsFromDatabase();
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
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_subtitle"),
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
                    Title = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_title"),
                    SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_subtitle"),
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
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_delete_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_delete_subtitle"),
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
        return Context.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_name");
    }

    public string GetTranslatedPluginDescription()
    {
        return Context.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_description");
    }

    public void OnCultureInfoChanged(CultureInfo cultureInfo)
    {
        Context.API.LogDebug(ClassName, $"Culture info changed to {cultureInfo}");
        CultureInfo = cultureInfo;
        CultureInfoChanged?.Invoke(this, cultureInfo);
    }

    #endregion

    #region ISavable Interface

    // Warning: This method will be called after dispose.
    public void Save()
    {
        // We don't need to save plugin settings because all settings will be saved.
        //Context?.API.SaveSettingJsonStorage<Settings>();
    }

    #endregion

    #region ISettingProvider Interface

    public Control CreateSettingPanel()
    {
        Context.API.LogWarn(ClassName, $"{Settings}");
        return new SettingsPanel(this);
    }

    #endregion

    #region Clipboard Monitor

    private async void OnClipboardChange(object? sender, ClipboardMonitor.ClipboardChangedEventArgs e)
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
            Title = "",
            DataType = e.DataType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            CachedImagePath = string.Empty,
            Score = CurrentScore + 1,
            InitScore = CurrentScore + 1,
            Pinned = false,
            CreateTime = now,
        };
        CurrentScore++;

        // process clipboard data
        switch (e.DataType)
        {
            case DataType.Text:
                clipboardData.Text = ClipboardMonitor.ClipboardText;
                Context.API.LogDebug(ClassName, "Processed text change");
                break;
            case DataType.Image:
                clipboardData.Text = $"Image: {clipboardData.CreateTime.ToString(CultureInfo)}";
                if (Settings.CacheImages)
                {
                    var imageName = StringUtils.FormatImageName(Settings.CacheFormat, clipboardData.CreateTime,
                        clipboardData.SenderApp ?? Context.GetTranslation("flowlauncher_plugin_clipboardplus_unknown_app"));
                    var imagePath = FileUtils.SaveImageCache(clipboardData, PathHelper.ImageCachePath, imageName);
                    clipboardData.CachedImagePath = imagePath;
                }
                Context.API.LogDebug(ClassName, "Processed image change");
                break;
            case DataType.Files:
                var filesStrings = ClipboardMonitor.ClipboardFiles.ToArray();
                clipboardData.Data = filesStrings;
                clipboardData.Text = string.Join("\n", filesStrings.Take(2)) + "\n...";
                Context.API.LogDebug(ClassName, "Processed file change");
                break;
            case DataType.Other:
                // Clipboard monitor for type not in Text, Image, Files are not implemented now.
                Context.API.LogDebug(ClassName, "Other change listened, skip");
                return;
            default:
                break;
        }
        clipboardData.Title = MyRegex().Replace(clipboardData.Text.Trim(), "");

        // add to list and database if no repeat 
        if (RecordsList.Any(record => record.DataMd5 == clipboardData.DataMd5))
        {
            return;
        }
        RecordsList.AddFirst(clipboardData);
        Context.API.LogDebug(ClassName, "Added to list");

        // add to database if needed
        var needAddDatabase =
            Settings.KeepText && clipboardData.DataType == DataType.Text
            || Settings.KeepImages && clipboardData.DataType == DataType.Image
            || Settings.KeepFiles && clipboardData.DataType == DataType.Files;
        if (needAddDatabase)
        {
            await DatabaseHelper.AddOneRecordAsync(clipboardData);
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

    private async Task InitRecordsFromDatabase()
    {
        // clear expired records
        try
        {
            foreach (var pair in Settings.KeepTimePairs)
            {
                Context.API.LogInfo(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                await DatabaseHelper.DeleteRecordByKeepTimeAsync(
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
        var records = await DatabaseHelper.GetAllRecordAsync();
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
        var dispSubTitle = $"{clipboardData.CreateTime.ToString(CultureInfo)}: {clipboardData.SenderApp}";
        dispSubTitle = clipboardData.Pinned ? $"{PinUnicode}{dispSubTitle}" : dispSubTitle;
        return new Result
        {
            Title = clipboardData.Title,
            SubTitle = dispSubTitle,
            Icon = () => clipboardData.Icon,
            Glyph = clipboardData.Glyph,
            CopyText = clipboardData.Text,
            Score = GetNewScoreByOrderBy(clipboardData),
            TitleToolTip = clipboardData.Text,
            SubTitleToolTip = dispSubTitle,
            ContextData = clipboardData,
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, clipboardData)),
            Action = _ =>
            {
                switch (Settings.ClickAction)
                {
                    case ClickAction.CopyPaste:
                        CopyToClipboard(clipboardData);
                        Context.API.VisibilityChanged += Paste_VisibilityChanged;
                        Context.API.HideMainWindow();
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
                        Context.API.VisibilityChanged += Paste_VisibilityChanged;
                        Context.API.HideMainWindow();
                        RemoveFromList(clipboardData, false);
                        break;
                    case ClickAction.CopyPasteDeleteListDatabase:
                        CopyToClipboard(clipboardData);
                        Context.API.VisibilityChanged += Paste_VisibilityChanged;
                        Context.API.HideMainWindow();
                        RemoveFromListDatabase(clipboardData, false);
                        break;
                    default:
                        CopyToClipboard(clipboardData);
                        break;
                }
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard"), 
                    StringUtils.CompressString(clipboardData.Text, 36));
                return true;
            },
        };
    }

    private async void Paste_VisibilityChanged(object sender, VisibilityChangedEventArgs args)
    {
        if (args.IsVisible == false)
        {
            await Task.Delay(100);
            new InputSimulator().Keyboard.ModifiedKeyStroke(
                VirtualKeyCode.CONTROL,
                VirtualKeyCode.VK_V
            );
            Context.API.VisibilityChanged -= Paste_VisibilityChanged;
        }
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
                score = (int)clipboardData.DataType;
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
        // TODO: Add support for files.
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
        await DatabaseHelper.DeleteOneRecordAsync(clipboardData);
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
        await DatabaseHelper.PinOneRecordAsync(clipboardData);
        // TODO: Ask Flow-Launcher for a better way to refresh the query.
        if (needEsc)
        {
            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        }
        Context.API.ReQuery(false);
    }

    #endregion

    #region IClipboardPlus Interface

    PluginInitContext? IClipboardPlus.Context => Context;

    ISettings IClipboardPlus.Settings => Settings;

    public ISettings LoadSettingJsonStorage()
    {
        return Settings;
    }

    public void SaveSettingJsonStorage()
    {
        Context.API.SaveSettingJsonStorage<Settings>();
    }

    CultureInfo IClipboardPlus.CultureInfo => CultureInfo;

    public event EventHandler<CultureInfo>? CultureInfoChanged;

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
            if (DatabaseHelper != null)
            {
                DatabaseHelper?.Dispose();
                DatabaseHelper = null!;
                Context.API.LogDebug(ClassName, $"Disposed DatabaseHelper");
            }
            ClipboardMonitor.ClipboardChanged -= OnClipboardChange;
            ClipboardMonitor.Dispose();
            ClipboardMonitor = null!;
            Context.API.LogDebug(ClassName, $"Disposed ClipboardMonitor");
            CultureInfoChanged = null;
            Settings = null!;
            RecordsList = null!;
            Context.API.LogWarn(ClassName, $"Finish dispose");
        }
    }

    #endregion
}
