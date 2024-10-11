using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowsInput;
using Clipboard = System.Windows.Clipboard;

namespace Flow.Launcher.Plugin.ClipboardPlus;

public class ClipboardPlus : IAsyncPlugin, IAsyncReloadable, IContextMenu, IPluginI18n,
    IResultUpdated, ISavable, ISettingProvider, IClipboardPlus, IDisposable
{
    #region Properties

    // Plugin context
    private PluginInitContext Context = null!;

    // Class name for logging
    private string ClassName => GetType().Name;

    // Culture info
    private CultureInfo CultureInfo = null!;

    // Settings
    private Settings Settings = null!;

    // Database helper
    private DatabaseHelper DatabaseHelper = null!;

    // Clipboard monitor instance
    // Warning: Do not init the instance in InitAsync function! This will cause issues.
    private ClipboardMonitor ClipboardMonitor = new() { ObserveLastEntry = false };

    // Records list & Score
    private LinkedList<ClipboardData> RecordsList = new();
    private int CurrentScore = 0;

    // Score interval
    // Note: Get scores of the items further apart to make sure the ranking seqence of items is correct.
    // https://github.com/Flow-Launcher/Flow.Launcher/issues/2904
    private const int ScoreInterval = 10000;

    // Clipboard retry times
    private const int ClipboardRetryTimes = 5;

    // Retry interval
    private const int RetryInterval = 100;

    #region Scores

    private const int ScoreInterval1 = 1 * ScoreInterval;
    private const int ScoreInterval2 = 2 * ScoreInterval;
    private const int ScoreInterval3 = 3 * ScoreInterval;
    private const int ScoreInterval4 = 4 * ScoreInterval;
    private const int ScoreInterval5 = 5 * ScoreInterval;

    private const int CleanActionScore = ClipboardData.MaximumScore + 2 * ScoreInterval;
    private const int ClearActionScore = ClipboardData.MaximumScore + 1 * ScoreInterval;

    #endregion

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
                        Score = ScoreInterval4,
                        Action = _ =>
                        {
                            var number = DeleteAllRecordsFromList();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(
                                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_msg_subtitle"), number));
                                return true;
                            }
                            else
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                return false;
                            }
                        },
                    },
                    new Result
                    {
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_subtitle"),
                        IcoPath = PathHelper.DatabaseIconPath,
                        Glyph = ResourceHelper.DatabaseGlyph,
                        Score = ScoreInterval3,
                        AsyncAction = async _ =>
                        {
                            var number = await DeleteAllRecordsFromListDatabaseAsync();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(
                                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_msg_subtitle"), number));
                                return true;
                            }
                            else
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                return false;
                            }
                        }
                    },
                    new Result
                    {
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_subtitle"),
                        IcoPath = PathHelper.UnpinIcon1Path,
                        Glyph = ResourceHelper.UnpinGlyph,
                        Score = ScoreInterval2,
                        AsyncAction = async _ =>
                        {
                            var number = await DeleteUnpinnedRecordsFromListDatabaseAsync();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(
                                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_msg_subtitle"), number));
                                return true;
                            }
                            else
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                return false;
                            }
                        }
                    },
                    new Result
                    {
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_subtitle"),
                        IcoPath = PathHelper.ErrorIconPath,
                        Glyph = ResourceHelper.ErrorGlyph,
                        Score = ScoreInterval1,
                        AsyncAction = async _ =>
                        {
                            var number = await DeleteInvalidRecordsFromListDatabaseAsync();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(
                                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_msg_subtitle"), number));
                                return true;
                            }
                            else
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                return false;
                            }
                        }
                    }
                }
            );
        }
        else
        {
            // clean action
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clean_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clean_subtitle"),
                IcoPath = PathHelper.CleanIconPath,
                Glyph = ResourceHelper.CleanGlyph,
                Score = CleanActionScore,
                Action = _ =>
                {
                    Clipboard.Clear();
                    return false;
                },
            });

            // clear action
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_subtitle"),
                IcoPath = PathHelper.ClearIconPath,
                Glyph = ResourceHelper.ClearGlyph,
                Score = ClearActionScore,
                Action = _ =>
                {
                    Context.API.ChangeQuery($"{query.ActionKeyword}{Plugin.Query.TermSeparator}{Settings.ClearKeyword} ", true);
                    return false;
                },
            });

            // update results
            ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs
            {
                Results = results,
                Query = query
            });

            // records results
            var records = query.Search.Trim().Length == 0
                ? RecordsList.ToArray()
                : RecordsList.Where(i => !string.IsNullOrEmpty(i.GetText(CultureInfo)) && i.GetText(CultureInfo).ToLower().Contains(query.Search.Trim().ToLower())).ToArray();
            results.AddRange(records.Select(GetResultFromClipboardData));
            Context.API.LogDebug(ClassName, "Added records successfully");
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

        // init encrypt key
        StringUtils.InitEncryptKey(Settings.EncryptKey);

        // init database & records
        var fileExists = File.Exists(PathHelper.DatabasePath);
        DatabaseHelper = new DatabaseHelper(PathHelper.DatabasePath, context: context);
        await DatabaseHelper.InitializeDatabaseAsync();
        if (fileExists)
        {
            await InitRecordsFromDatabaseAsync();
        }
        Context.API.LogDebug(ClassName, "Init database successfully");

        // init clipboard monitor
        ClipboardMonitor.ClipboardChanged += OnClipboardChange;
        ClipboardMonitor.StartMonitoring();
        Context.API.LogDebug(ClassName, "Init clipboard monitor successfully");
    }

    #endregion

    #region IAsyncReloadable Interface

    public async Task ReloadDataAsync()
    {
        // reload records
        await InitRecordsFromDatabaseAsync();
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

        // Copy & Pin & Delete
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
                    Score = ScoreInterval5,
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
                    Score = ScoreInterval2,
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
                    Score = ScoreInterval1,
                    Action = _ =>
                    {
                        RemoveFromListDatabase(clipboardData, true);
                        return false;
                    }
                },
            }
        );

        // Save
        var saved = clipboardData.Saved;
        if (!clipboardData.Saved)
        {
            Context.API.LogInfo(ClassName, $"Clipboard data: {clipboardData}, Saved: {clipboardData.Saved}");
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_save_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_save_subtitle"),
                IcoPath = PathHelper.DatabaseIconPath,
                Glyph = ResourceHelper.DatabaseGlyph,
                Score = ScoreInterval3,
                Action = _ =>
                {
                    SaveToDatabase(clipboardData, true);
                    return false;
                }
            });
        }

        // Copy as plain text
        if (clipboardData.DataType == DataType.RichText)
        {
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_subtitle"),
                IcoPath = PathHelper.CopyIconPath,
                Glyph = ResourceHelper.CopyGlyph,
                Score = ScoreInterval4,
                Action = _ =>
                {
                    CopyAsPlainTextToClipboard(clipboardData);
                    return true;
                }
            });
        }
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

    #region IResultUpdated Interface

    public event ResultUpdatedEventHandler? ResultsUpdated;

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
        if (e.Content is null || e.DataType == DataType.Other)
        {
            return;
        }

        // init clipboard data
        var now = DateTime.Now;
        var dataType = e.DataType;
        var saved = Settings.KeepText && dataType == DataType.UnicodeText
            || Settings.KeepText && dataType == DataType.RichText
            || Settings.KeepImages && dataType == DataType.Image
            || Settings.KeepFiles && dataType == DataType.Files;
        var clipboardData = new ClipboardData(e.Content, dataType, Settings.EncryptData)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = e.SourceApplication.Name,
            InitScore = CurrentScore + ScoreInterval,
            CreateTime = now,
            Pinned = false,
            Saved = saved
        };

        // filter duplicate data
        if (RecordsList.Count != 0 && RecordsList.First().DataEquals(clipboardData))
        {
            return;
        }

        // process clipboard data
        if (dataType == DataType.Image && Settings.CacheImages)
        {
            var imageName = StringUtils.FormatImageName(Settings.CacheFormat, clipboardData.CreateTime,
                clipboardData.SenderApp ?? Context.GetTranslation("flowlauncher_plugin_clipboardplus_unknown_app"));
            var imagePath = FileUtils.SaveImageCache(clipboardData, PathHelper.ImageCachePath, imageName);
            clipboardData.CachedImagePath = imagePath;
        }
        if (dataType == DataType.RichText)
        {
            // due to some bugs, we need to convert rtf to plain text
            if (string.IsNullOrEmpty(ClipboardMonitor.ClipboardText))
            {
                clipboardData.UnicodeText = StringUtils.ConvertRtfToPlainText(ClipboardMonitor.ClipboardRtfText);
            }
            else
            {
                clipboardData.UnicodeText = ClipboardMonitor.ClipboardText;
            }
        }

        // add to list and database if no repeat
        RecordsList.AddFirst(clipboardData);
        Context.API.LogDebug(ClassName, "Added to list");

        // update score
        CurrentScore += ScoreInterval;

        // save to database if needed
        if (saved)
        {
            await DatabaseHelper.AddOneRecordAsync(clipboardData);
        }
        Context.API.LogDebug(ClassName, "Added to database");

        // remove last record if needed
        if (RecordsList.Count >= Settings.MaxRecords)
        {
            RecordsList.RemoveLast();
        }
    }

    #endregion

    #region List & Database

    private async Task InitRecordsFromDatabaseAsync()
    {
        // clear expired records
        try
        {
            foreach (var pair in Settings.KeepTimePairs)
            {
                Context.API.LogInfo(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                await DatabaseHelper.DeleteRecordsByKeepTimeAsync(
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
        var records = await DatabaseHelper.GetAllRecordsAsync();
        if (records.Count > 0)
        {
            RecordsList = records;
            CurrentScore = records.Max(r => r.InitScore);
        }
        Context.API.LogWarn(ClassName, "Restored records successfully");
    }

    private int DeleteAllRecordsFromList()
    {
        var number = RecordsList.Count;
        RecordsList.Clear();
        return number;
    }

    private async Task<int> DeleteAllRecordsFromListDatabaseAsync()
    {
        var number = RecordsList.Count;
        RecordsList.Clear();
        await DatabaseHelper.DeleteAllRecordsAsync();
        CurrentScore = 1;
        return number;
    }

    private async Task<int> DeleteUnpinnedRecordsFromListDatabaseAsync()
    {
        var unpinnedRecords = RecordsList.Where(r => !r.Pinned).ToArray();
        var number = unpinnedRecords.Length;
        foreach (var record in unpinnedRecords)
        {
            RecordsList.Remove(record);
        }
        await DatabaseHelper.DeleteUnpinnedRecordsAsync();
        if (RecordsList.Any())
        {
            CurrentScore = RecordsList.Max(r => r.InitScore);
        }
        else
        {
            CurrentScore = 1;
        }
        return number;
    }

    private async Task<int> DeleteInvalidRecordsFromListDatabaseAsync()
    {
        var invalidRecords = RecordsList.Where(r => r.DataToValid() is null).ToArray();
        var number = invalidRecords.Length;
        foreach (var record in invalidRecords)
        {
            RecordsList.Remove(record);
            await DatabaseHelper.DeleteOneRecordAsync(record);
        }
        if (RecordsList.Any())
        {
            CurrentScore = RecordsList.Max(r => r.InitScore);
        }
        else
        {
            CurrentScore = 1;
        }
        return number;
    }

    #endregion

    #region Query Result

    private Result GetResultFromClipboardData(ClipboardData clipboardData)
    {
        if (clipboardData.Pinned)
        {
            Context.API.LogInfo(ClassName, $"Pinned record: {clipboardData}");
            var score = clipboardData.GetScore(Settings.RecordOrder);
            Context.API.LogInfo(ClassName, $"Pinned record score: {score}");
        }
        return new Result
        {
            Title = clipboardData.GetTitle(CultureInfo),
            SubTitle = clipboardData.GetSubtitle(CultureInfo),
            SubTitleToolTip = clipboardData.GetSubtitle(CultureInfo),
            Icon = () => clipboardData.Icon,
            Glyph = clipboardData.Glyph,
            CopyText = clipboardData.GetText(CultureInfo),
            Score = clipboardData.GetScore(Settings.RecordOrder),
            TitleToolTip = clipboardData.GetText(CultureInfo),
            ContextData = clipboardData,
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, clipboardData)),
            AsyncAction = async _ =>
            {
                switch (Settings.ClickAction)
                {
                    case ClickAction.Copy:
                        CopyToClipboard(clipboardData);
                        break;
                    case ClickAction.CopyPaste:
                        Context.API.HideMainWindow();
                        CopyToClipboard(clipboardData);
                        await WaitWindowHideAndSimulatePaste();
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
                        Context.API.HideMainWindow();
                        CopyToClipboard(clipboardData);
                        RemoveFromList(clipboardData, false);
                        await WaitWindowHideAndSimulatePaste();
                        break;
                    case ClickAction.CopyPasteDeleteListDatabase:
                        Context.API.HideMainWindow();
                        CopyToClipboard(clipboardData);
                        RemoveFromListDatabase(clipboardData, false);
                        await WaitWindowHideAndSimulatePaste();
                        break;
                    default:
                        break;
                }
                return true;
            },
        };
    }

    private async Task WaitWindowHideAndSimulatePaste()
    {
        while (Context.API.IsMainWindowVisible())
        {
            await Task.Delay(RetryInterval);
        }
        new InputSimulator().Keyboard.ModifiedKeyStroke(
            VirtualKeyCode.CONTROL,
            VirtualKeyCode.VK_V
        );
    }

    #endregion

    #region Clipboard Actions

    private async void SaveToDatabase(ClipboardData clipboardData, bool requery)
    {
        if (!clipboardData.Saved)
        {
            clipboardData.Saved = true;
            RecordsList.Remove(clipboardData);
            RecordsList.AddLast(clipboardData);
            await DatabaseHelper.AddOneRecordAsync(clipboardData);
            if (requery)
            {
                ReQuery();
            }
        }
    }

    private async void CopyToClipboard(ClipboardData clipboardData)
    {
        var validObject = clipboardData.DataToValid();
        var dataType = clipboardData.DataType;
        if (validObject is not null)
        {
            var exception = await RetryAction(() =>
            {
                switch (dataType)
                {
                    case DataType.UnicodeText:
                        Clipboard.SetText((string)validObject);
                        break;
                    case DataType.RichText:
                        Clipboard.SetText((string)validObject, TextDataFormat.Rtf);
                        break;
                    case DataType.Image:
                        Clipboard.SetImage((BitmapSource)validObject);
                        break;
                    case DataType.Files:
                        var paths = new StringCollection();
                        paths.AddRange((string[])validObject);
                        Clipboard.SetFileDropList(paths);
                        break;
                    default:
                        break;
                }
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo), 54));
            }
            else
            {
                Context.API.LogException(ClassName, "Copy to clipboard failed", exception);
                Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard_exception"));
            }
        }
        else
        {
            switch (dataType)
            {
                case DataType.UnicodeText:
                case DataType.RichText:
                    Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_text_data_invalid"));
                    break;
                case DataType.Image:
                    Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_image_data_invalid"));
                    break;
                case DataType.Files:
                    Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                        Context.GetTranslation("flowlauncher_plugin_clipboardplus_files_data_invalid"));
                    break;
            }
        }
    }

    private async void CopyAsPlainTextToClipboard(ClipboardData clipboardData)
    {
        var dataType = clipboardData.DataType;
        if (dataType != DataType.RichText)
        {
            return;
        }

        var validObject = clipboardData.UnicodeTextToValid();
        if (validObject is not null)
        {
            var exception = await RetryAction(() => Clipboard.SetText(validObject));
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo), 54));
            }
            else
            {
                Context.API.LogException(ClassName, "Copy to clipboard failed", exception);
                Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard_exception"));
            }
        }
        else
        {
            Context.API.ShowMsgError(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                Context.GetTranslation("flowlauncher_plugin_clipboardplus_text_data_invalid"));
        }
    }

    private void RemoveFromList(ClipboardData clipboardData, bool requery)
    {
        RecordsList.Remove(clipboardData);
        if (requery)
        {
            ReQuery();
        }
    }

    private async void RemoveFromListDatabase(ClipboardData clipboardData, bool requery)
    {
        RecordsList.Remove(clipboardData);
        await DatabaseHelper.DeleteOneRecordAsync(clipboardData);
        if (requery)
        {
            ReQuery();
        }
    }

    private async void PinOneRecord(ClipboardData clipboardData, bool requery)
    {
        clipboardData.Pinned = !clipboardData.Pinned;
        RecordsList.Remove(clipboardData);
        RecordsList.AddLast(clipboardData);
        await DatabaseHelper.PinOneRecordAsync(clipboardData);
        if (requery)
        {
            ReQuery();
        }
    }

    private async void ReQuery()
    {
        // TODO: Ask Flow-Launcher for a better way to exit the context menu.
        new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        await Task.Delay(RetryInterval);
        Context.API.ReQuery(false);
    }

    private static async Task<Exception?> FlushClipboard()
    {
        return await RetryAction(Clipboard.Flush);
    }

    private static async Task<Exception?> RetryAction(Action action)
    {
        for (int i = 0; i < ClipboardRetryTimes; i++)
        {
            try
            {
                action();
                break;
            }
            catch (Exception e)
            {
                if (i == ClipboardRetryTimes - 1)
                {
                    return e;
                }
                await Task.Delay(RetryInterval);
            }
        }
        return null;
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

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async void Dispose(bool disposing)
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
            var exception = await FlushClipboard();
            if (exception == null)
            {
                Context.API.LogDebug(ClassName, $"Flushed Clipboard");
            }
            else
            {
                Context.API.LogException(ClassName, $"Flushed Clipboard failed", exception);
            }
            Context.API.LogWarn(ClassName, $"Finish dispose");
            _disposed = true;
        }
    }

    #endregion
}
