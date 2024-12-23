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
    IResultUpdated, ISavable, ISettingProvider, IClipboardPlus, IAsyncDisposable
{
    #region Properties

    // Plugin context
    private PluginInitContext Context = null!;

    // Class name for logging
    private string ClassName => GetType().Name;

    // Culture info
    private CultureInfo CultureInfo = null!;

    // Settings
    private ISettings Settings = null!;

    // Database helper
    private SqliteDatabase Database = null!;

    // Clipboard monitor instance
    // Warning: Do not init the instance in InitAsync function! This will cause issues.
    private ClipboardMonitorW ClipboardMonitor = new()
    {
        ObserveLastEntry = false,
        ObservableFormats = new()
        {
            Images = true,
            Texts = true,
            Files = true,
            Others = false
        }
    };

    // Records list & Score
    private LinkedList<ClipboardDataPair> RecordsList = new();

    // Score interval
    // Note: Get scores of the items further apart to make sure the ranking seqence of items is correct.
    // https://github.com/Flow-Launcher/Flow.Launcher/issues/2904
    private const int ScoreInterval = 10000;

    // Clipboard retry times
    private const int ClipboardRetryTimes = 5;

    // Retry interval
    private const int RetryInterval = 100;

    // Max length of string
    private const int StringMaxLength = 54;

    #region Scores

    private const int ScoreInterval1 = 1 * ScoreInterval;
    private const int ScoreInterval2 = 2 * ScoreInterval;
    private const int ScoreInterval3 = 3 * ScoreInterval;
    private const int ScoreInterval4 = 4 * ScoreInterval;
    private const int ScoreInterval5 = 5 * ScoreInterval;
    private const int ScoreInterval6 = 6 * ScoreInterval;
    private const int ScoreInterval7 = 7 * ScoreInterval;
    private const int ScoreInterval8 = 8 * ScoreInterval;
    private const int ScoreInterval9 = 9 * ScoreInterval;

    private const int TopActionScore1 = 2 * ClipboardData.MaximumScore + 3 * ScoreInterval;
    private const int TopActionScore2 = 2 * ClipboardData.MaximumScore + 2 * ScoreInterval;
    private const int TopActionScore3 = 2 * ClipboardData.MaximumScore + 1 * ScoreInterval;

    private const int BottomActionScore1 = 7500;
    private const int BottomActionScore2 = 5000;
    private const int BottomActionScore3 = 2500;

    #endregion

    #endregion

    #region IAsyncPlugin Interface

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        return Task.Run(() => Query(query));
    }

    // TODO: Remove selected count from score.
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
                                    string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_msg_subtitle"), number));
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
                        Action = _ =>
                        {
                            var number = DeleteAllRecordsFromListDatabase();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_msg_subtitle"), number));
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
                        Action = _ =>
                        {
                            var number = DeleteUnpinnedRecordsFromListDatabase();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_msg_subtitle"), number));
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
                        Action = _ =>
                        {
                            var number = DeleteInvalidRecordsFromListDatabase();
                            if (number != 0)
                            {
                                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                    string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_msg_subtitle"), number));
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
                Score = Settings.ActionTop ? TopActionScore1 : BottomActionScore1,
                AsyncAction = async _ =>
                {
                    await Win32Helper.StartSTATaskAsync(Clipboard.Clear);
                    return true;
                },
            });

            // connect & disconnect action
            if (ClipboardMonitor.MonitorClipboard)
            {
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_disconnect_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_disconnect_subtitle"),
                    IcoPath = PathHelper.DisconnectIconPath,
                    Glyph = ResourceHelper.DisconnectGlyph,
                    Score = Settings.ActionTop ? TopActionScore2 : BottomActionScore2,
                    Action = _ =>
                    {
                        ClipboardMonitor.PauseMonitoring();
                        return true;
                    },
                });
            }
            else
            {
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_connect_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_connect_subtitle"),
                    IcoPath = PathHelper.ConnectIconPath,
                    Glyph = ResourceHelper.ConnectGlyph,
                    Score = Settings.ActionTop ? TopActionScore2 : BottomActionScore2,
                    Action = _ =>
                    {
                        ClipboardMonitor.ResumeMonitoring();
                        return true;
                    },
                });
            }

            // clear action
            // TODO: Fix this when SettingsKeyword is all whitespaces.
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_subtitle"),
                IcoPath = PathHelper.ClearIconPath,
                Glyph = ResourceHelper.ClearGlyph,
                Score = Settings.ActionTop ? TopActionScore3 : BottomActionScore3,
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
                : RecordsList.Where(i => !string.IsNullOrEmpty(i.ClipboardData.GetText(CultureInfo)) && i.ClipboardData.GetText(CultureInfo).ToLower().Contains(query.Search.Trim().ToLower())).ToArray();
            foreach (var record in records)
            {
                var result = GetResultFromClipboardData(record);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            Context.API.LogDebug(ClassName, "Added records successfully");
        }
        return results;
    }

    public async Task InitAsync(PluginInitContext context)
    {
        Context = context;
        ClipboardMonitor.SetContext(context);

        // init path helper
        PathHelper.Init(context);

        // init settings
        Settings = context.API.LoadSettingJsonStorage<Settings>();
        Context.API.LogDebug(ClassName, "Init settings successfully");
        Context.API.LogInfo(ClassName, $"{Settings}");

        // init encrypt key
        StringUtils.InitEncryptKey(Settings.EncryptKey);

        // init database & records
        var fileExists = File.Exists(PathHelper.DatabasePath);
        Database = new SqliteDatabase(PathHelper.DatabasePath, scoreInterval: ScoreInterval, context: context);
        await Database.InitializeDatabaseAsync();
        if (fileExists)
        {
            await InitRecordsFromDatabaseAsync();
        }
        Context.API.LogDebug(ClassName, "Init database successfully");

        // init clipboard monitor
        ClipboardMonitor.ClipboardChanged += OnClipboardChange;
        ClipboardMonitor.StartMonitoring();  // just call it for ClipboardMonitorW
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

    // TODO: Remove selected count from score.
    public List<Result> LoadContextMenus(Result result)
    {
        var results = new List<Result>();
        if (result.ContextData is not ClipboardDataPair clipboardDataPair)
        {
            return results;
        }

        var clipboardData = clipboardDataPair.ClipboardData;

        // Copy Default Option
        results.AddRange(
            new[]
            {
                new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_subtitle"),
                    IcoPath = PathHelper.CopyIconPath,
                    Glyph = ResourceHelper.CopyGlyph,
                    Score = ScoreInterval9,
                    Action = _ =>
                    {
                        CopyToClipboard(clipboardDataPair);
                        return true;
                    }
                }
            }
        );

        // Copy Addition Options
        switch (clipboardData.DataType)
        {
            case DataType.UnicodeText:
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_subtitle"),
                    IcoPath = PathHelper.CopyIconPath,
                    Glyph = ResourceHelper.CopyGlyph,
                    Score = ScoreInterval8,
                    Action = _ =>
                    {
                        CopyOriginallyToClipboard(clipboardDataPair);
                        return true;
                    }
                });
                break;
            case DataType.RichText:
                results.AddRange(
                    new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_rich_text_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_rich_text_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval8,
                            Action = _ =>
                            {
                                CopyOriginallyToClipboard(clipboardDataPair);
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval7,
                            Action = _ =>
                            {
                                CopyAsPlainTextToClipboard(clipboardDataPair);
                                return true;
                            }
                        }
                    }
                );
                break;
            case DataType.Image:
                results.AddRange(
                    new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_image_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_image_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval8,
                            Action = _ =>
                            {
                                CopyOriginallyToClipboard(clipboardDataPair);
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_image_file_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_image_file_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval7,
                            Action = _ =>
                            {
                                CopyImageFileToClipboard(clipboardDataPair);
                                return true;
                            }
                        },
                    }
                );
                break;
            case DataType.Files:
                results.AddRange(
                    new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_files_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_files_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval8,
                            Action = _ =>
                            {
                                CopyOriginallyToClipboard(clipboardDataPair);
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_sort_name_asc_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_sort_name_asc_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval7,
                            Action = _ =>
                            {
                                CopyBySortingNameToClipboard(clipboardDataPair, true);
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_sort_name_desc_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_sort_name_desc_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval6,
                            Action = _ =>
                            {
                                CopyBySortingNameToClipboard(clipboardDataPair, false);
                                return true;
                            }
                        }
                    }
                );

                var validObject = clipboardData.DataToValid();
                if (validObject is string[] filePaths && filePaths.Length == 1)
                {
                    results.AddRange(new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_file_path_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_file_path_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval5,
                            Action = _ =>
                            {
                                CopyFilePathToClipboard(clipboardDataPair, filePaths);
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_file_content_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_file_content_subtitle"),
                            IcoPath = PathHelper.CopyIconPath,
                            Glyph = ResourceHelper.CopyGlyph,
                            Score = ScoreInterval4,
                            Action = _ =>
                            {
                                CopyFileContentToClipboard(clipboardDataPair, filePaths);
                                return true;
                            }
                        }
                    });
                }
                break;
            default:
                break;
        }

        // Save
        var saved = clipboardData.Saved;
        if (!clipboardData.Saved)
        {
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_save_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_save_subtitle"),
                IcoPath = PathHelper.DatabaseIconPath,
                Glyph = ResourceHelper.DatabaseGlyph,
                Score = ScoreInterval3,
                Action = _ =>
                {
                    SaveToDatabase(clipboardDataPair, true);
                    return false;
                }
            });
        }

        // Pin & Delete
        var pinned = clipboardData.Pinned;
        var pinStr = pinned ? "unpin" : "pin";
        results.AddRange(
            new[]
            {
                new Result
                {
                    Title = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_title"),
                    SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_subtitle"),
                    IcoPath = PathHelper.GetPinIconPath(pinned),
                    Glyph = ResourceHelper.GetPinGlyph(pinned),
                    Score = ScoreInterval2,
                    Action = _ =>
                    {
                        PinOneRecord(clipboardDataPair, true);
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
                        RemoveFromListDatabase(clipboardDataPair, true);
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

    private void OnClipboardChange(object? sender, ClipboardMonitorW.ClipboardChangedEventArgs e)
    {
        Context.API.LogDebug(ClassName, "Clipboard changed");
        if (e.Content is null || e.DataType == DataType.Other || sender is not ClipboardMonitorW clipboardMonitor)
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
            InitScore = Database.CurrentScore,
            CachedImagePath = string.Empty,
            CreateTime = now,
            Pinned = false,
            Saved = saved,
            UnicodeText = string.Empty,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5
        };

        // filter duplicate data
        if (RecordsList.Count != 0 && RecordsList.First().ClipboardData.DataEquals(clipboardData))
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
            if (string.IsNullOrEmpty(clipboardMonitor.ClipboardText))
            {
                clipboardData.UnicodeText = StringUtils.ConvertRtfToPlainText(clipboardMonitor.ClipboardRtfText);
            }
            else
            {
                clipboardData.UnicodeText = clipboardMonitor.ClipboardText;
            }
        }

        // add to list and database if no repeat
        RecordsList.AddFirst(new ClipboardDataPair() 
        { 
            ClipboardData = clipboardData, 
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, clipboardData)) 
        });
        Context.API.LogDebug(ClassName, "Added to list");

        // update score
        Database.CurrentScore += ScoreInterval;

        // save to database if needed
        if (saved)
        {
            _ = Database.AddOneRecordAsync(clipboardData, true);  // no need to wait
        }
        Context.API.LogDebug(ClassName, "Added to database");

        // remove last record if needed
        if (RecordsList.Count >= Settings.MaxRecords)
        {
            RecordsList.Last?.Value.Dispose();
            RecordsList.RemoveLast();
        }

        // collect garbage
        GarbageCollect();
    }

    #endregion

    #region List & Database

    public async Task InitRecordsFromDatabaseAsync()
    {
        // clear expired records
        try
        {
            foreach (var pair in Settings.KeepTimePairs)
            {
                Context.API.LogDebug(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                await Database.DeleteRecordsByKeepTimeAsync(
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
        var records = await Database.GetAllRecordsAsync(true);
        if (records.Any())
        {
            var records1 = records.Select(record => new ClipboardDataPair()
            {
                ClipboardData = record,
                PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, record))
            });
            RecordsList = new LinkedList<ClipboardDataPair>(records1);
        }
        Context.API.LogWarn(ClassName, "Restored records successfully");
    }

    private int DeleteAllRecordsFromList()
    {
        var number = RecordsList.Count;
        foreach (var record in RecordsList)
        {
            record.Dispose();
        }
        RecordsList.Clear();
        GarbageCollect();
        return number;
    }

    private int DeleteAllRecordsFromListDatabase()
    {
        var number = RecordsList.Count;
        foreach (var record in RecordsList)
        {
            record.Dispose();
        }
        RecordsList.Clear();
        _ = Database.DeleteAllRecordsAsync();
        Database.CurrentScore = 1;
        GarbageCollect();
        return number;
    }

    private int DeleteUnpinnedRecordsFromListDatabase()
    {
        var unpinnedRecords = RecordsList.Where(r => !r.ClipboardData.Pinned).ToArray();
        var number = unpinnedRecords.Length;
        foreach (var record in unpinnedRecords)
        {
            record.Dispose();
            RecordsList.Remove(record);
        }
        _ = Database.DeleteUnpinnedRecordsAsync();
        if (RecordsList.Any())
        {
            Database.CurrentScore = RecordsList.Max(r => r.ClipboardData.InitScore);
        }
        else
        {
            Database.CurrentScore = 1;
        }
        GarbageCollect();
        return number;
    }

    private int DeleteInvalidRecordsFromListDatabase()
    {
        var invalidRecords = RecordsList.Where(r => r.ClipboardData.DataToValid() is null).ToArray();
        var number = invalidRecords.Length;
        foreach (var record in invalidRecords)
        {
            record.Dispose();
            RecordsList.Remove(record);
            _ = Database.DeleteOneRecordAsync(record.ClipboardData);
        }
        if (RecordsList.Any())
        {
            Database.CurrentScore = RecordsList.Max(r => r.ClipboardData.InitScore);
        }
        else
        {
            Database.CurrentScore = 1;
        }
        GarbageCollect();
        return number;
    }

    private void SaveToDatabase(ClipboardDataPair clipboardDataPair, bool requery)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        if (!clipboardData.Saved)
        {
            clipboardData.Saved = true;
            RecordsList.Remove(clipboardDataPair);
            RecordsList.AddFirst(clipboardDataPair);
            if (requery)
            {
                ReQuery();
            }
            _ = Database.AddOneRecordAsync(clipboardData, true);
        }
    }

    private void RemoveFromList(ClipboardDataPair clipboardDataPair, bool requery)
    {
        if (RecordsList.First?.Value == clipboardDataPair)
        {
            Database.CurrentScore -= ScoreInterval;
        }
        clipboardDataPair.Dispose();
        RecordsList.Remove(clipboardDataPair);
        if (requery)
        {
            ReQuery();
        }
        GarbageCollect();
    }

    private void RemoveFromListDatabase(ClipboardDataPair clipboardDataPair, bool requery)
    {
        if (RecordsList.First?.Value == clipboardDataPair)
        {
            Database.CurrentScore -= ScoreInterval;
        }
        var clipboardData = clipboardDataPair.ClipboardData;
        clipboardDataPair.Dispose();
        RecordsList.Remove(clipboardDataPair);
        if (requery)
        {
            ReQuery();
        }
        _ = Database.DeleteOneRecordAsync(clipboardData);
        GarbageCollect();
    }

    private void PinOneRecord(ClipboardDataPair clipboardDataPair, bool requery)
    {
        clipboardDataPair.TogglePinned();
        RecordsList.Remove(clipboardDataPair);
        RecordsList.AddFirst(clipboardDataPair);
        if (requery)
        {
            ReQuery();
        }
        _ = Database.PinOneRecordAsync(clipboardDataPair.ClipboardData);
    }

    private async void ReQuery()
    {
        // TODO: Improve refresh way here in future version of FL.
        new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
        await Task.Delay(RetryInterval);
        Context.API.ReQuery(false);
    }

    #endregion

    #region Query Result

    // TODO: Remove selected count from score.
    private Result? GetResultFromClipboardData(ClipboardDataPair clipboardDataPair)
    {
        try
        {
            var clipboardData = clipboardDataPair.ClipboardData;
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
                ContextData = clipboardDataPair,
                PreviewPanel = clipboardDataPair.PreviewPanel,
                AsyncAction = async _ =>
                {
                    switch (Settings.ClickAction)
                    {
                        case ClickAction.Copy:
                            CopyToClipboard(clipboardDataPair);
                            break;
                        case ClickAction.CopyPaste:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            await WaitWindowHideAndSimulatePaste();
                            break;
                        case ClickAction.CopyDeleteList:
                            CopyToClipboard(clipboardDataPair);
                            RemoveFromList(clipboardDataPair, false);
                            break;
                        case ClickAction.CopyDeleteListDatabase:
                            CopyToClipboard(clipboardDataPair);
                            RemoveFromListDatabase(clipboardDataPair, false);
                            break;
                        case ClickAction.CopyPasteDeleteList:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            RemoveFromList(clipboardDataPair, false);
                            await WaitWindowHideAndSimulatePaste();
                            break;
                        case ClickAction.CopyPasteDeleteListDatabase:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            RemoveFromListDatabase(clipboardDataPair, false);
                            await WaitWindowHideAndSimulatePaste();
                            break;
                        default:
                            break;
                    }
                    return true;
                },
            };
        }
        catch (Exception e)
        {
            Context.API.LogException(ClassName, "Get result from clipboard data failed", e);
            return null;
        }
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

    #region Default Copy

    private void CopyToClipboard(ClipboardDataPair clipboardDataPair)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var dataType = clipboardData.DataType;
        switch (dataType)
        {
            case DataType.UnicodeText:
                CopyOriginallyToClipboard(clipboardDataPair);
                break;
            case DataType.RichText:
                switch (Settings.DefaultRichTextCopyOption)
                {
                    case DefaultRichTextCopyOption.Rtf:
                        CopyOriginallyToClipboard(clipboardDataPair);
                        break;
                    case DefaultRichTextCopyOption.Plain:
                        CopyAsPlainTextToClipboard(clipboardDataPair);
                        break;
                    default:
                        break;
                }
                break;
            case DataType.Image:
                switch (Settings.DefaultImageCopyOption)
                {
                    case DefaultImageCopyOption.Image:
                        CopyOriginallyToClipboard(clipboardDataPair);
                        break;
                    case DefaultImageCopyOption.File:
                        CopyImageFileToClipboard(clipboardDataPair);
                        break;
                    default:
                        break;
                }
                break;
            case DataType.Files:
                switch (Settings.DefaultFilesCopyOption)
                {
                    case DefaultFilesCopyOption.Files:
                        CopyOriginallyToClipboard(clipboardDataPair);
                        break;
                    case DefaultFilesCopyOption.NameAsc:
                        CopyBySortingNameToClipboard(clipboardDataPair, true);
                        break;
                    case DefaultFilesCopyOption.NameDesc:
                        CopyBySortingNameToClipboard(clipboardDataPair, false);
                        break;
                    case DefaultFilesCopyOption.Path:
                        var validObject = clipboardData.DataToValid();
                        var filePaths = (validObject as string[])!;
                        if (filePaths.Length == 1)
                        {
                            CopyFilePathToClipboard(clipboardDataPair, filePaths);
                        }
                        else
                        {
                            CopyOriginallyToClipboard(clipboardDataPair);
                        }
                        break;
                    case DefaultFilesCopyOption.Content:
                        var validObject1 = clipboardData.DataToValid();
                        var filePaths1 = (validObject1 as string[])!;
                        if (filePaths1.Length == 1)
                        {
                            CopyFileContentToClipboard(clipboardDataPair, filePaths1);
                        }
                        else
                        {
                            CopyOriginallyToClipboard(clipboardDataPair);
                        }
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }

    #endregion

    #region Original Format

    private async void CopyOriginallyToClipboard(ClipboardDataPair clipboardDataPair)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var validObject = clipboardData.DataToValid();
        var dataType = clipboardData.DataType;
        if (validObject is not null)
        {
            var exception = await RetryActionOnSTAThreadAsync(() =>
            {
                switch (dataType)
                {
                    case DataType.UnicodeText:
                        Clipboard.Clear();
                        Clipboard.SetText((string)validObject);
                        break;
                    case DataType.RichText:
                        Clipboard.Clear();
                        Clipboard.SetText((string)validObject, TextDataFormat.Rtf);
                        break;
                    case DataType.Image:
                        Clipboard.Clear();
                        Clipboard.SetImage((BitmapSource)validObject);
                        break;
                    case DataType.Files:
                        Clipboard.Clear();
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
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo, validObject as string[]), StringMaxLength));
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

    #endregion

    #region Plain Text Format

    private async void CopyAsPlainTextToClipboard(ClipboardDataPair clipboardDataPair)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var dataType = clipboardData.DataType;
        if (dataType != DataType.RichText)
        {
            return;
        }

        var validObject = clipboardData.UnicodeTextToValid();
        if (validObject is not null)
        {
            var exception = await RetryActionOnSTAThreadAsync(() =>
            {
                Clipboard.Clear();
                Clipboard.SetText(validObject);
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo), StringMaxLength));
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

    #endregion

    #region Image File Format

    private async void CopyImageFileToClipboard(ClipboardDataPair clipboardDataPair)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var dataType = clipboardData.DataType;
        if (dataType != DataType.Image)
        {
            return;
        }

        var cachePath = string.Empty;
        if ((!string.IsNullOrEmpty(clipboardData.CachedImagePath)) && File.Exists(clipboardData.CachedImagePath))
        {
            cachePath = clipboardData.CachedImagePath;
        }
        else
        {
            cachePath = FileUtils.SaveImageCache(clipboardData, PathHelper.ImageCachePath, PathHelper.TempCacheImageName);
        }

        if (!string.IsNullOrEmpty(cachePath))
        {
            var exception = await RetryActionOnSTAThreadAsync(() =>
            {
                Clipboard.Clear();
                Clipboard.SetFileDropList(new StringCollection { cachePath });
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo), StringMaxLength));
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
                Context.GetTranslation("flowlauncher_plugin_clipboardplus_image_data_invalid"));
        }
    }

    #endregion

    #region Files Sorted Format

    private async void CopyBySortingNameToClipboard(ClipboardDataPair clipboardDataPair, bool ascend)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var dataType = clipboardData.DataType;
        if (dataType != DataType.Files)
        {
            return;
        }

        var validObject = clipboardData.DataToValid();
        if (validObject is not null)
        {
            var filePaths = ascend ? SortAscending((string[])validObject) : SortDescending((string[])validObject);
            var paths = new StringCollection();
            paths.AddRange(filePaths);
            var exception = await RetryActionOnSTAThreadAsync(() => 
            {
                Clipboard.Clear();
                Clipboard.SetFileDropList(paths);
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo, validObject as string[]), StringMaxLength));
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

    private static string[] SortAscending(string[] strings)
    {
        return strings.OrderBy(path => path, new NaturalStringComparer()).ToArray();
    }

    private static string[] SortDescending(string[] strings)
    {
        return strings.OrderByDescending(path => path, new NaturalStringComparer()).ToArray();
    }

    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            // If both strings are null, they are considered equal
            if (x == null && y == null) return 0;

            // If one string is null and the other is not, the null string is considered less
            if (x == null) return -1;
            if (y == null) return 1;

            // Compare strings by processing numeric segments as numbers
            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                // Skip leading non-numeric characters
                if (!char.IsDigit(x[i]) && !char.IsDigit(y[j]))
                {
                    if (x[i] != y[j]) return x[i] - y[j];
                    i++;
                    j++;
                    continue;
                }

                // Extract numeric segments
                var numX = ExtractNumber(x, ref i);
                var numY = ExtractNumber(y, ref j);

                // Compare numeric segments
                int result = numX.CompareTo(numY);
                if (result != 0) return result;
            }

            // Compare remaining non-numeric characters if one string is shorter
            return x.Length - y.Length;
        }

        private static int ExtractNumber(string s, ref int index)
        {
            int start = index;
            while (index < s.Length && char.IsDigit(s[index])) index++;
            if (start == index) return 0; // No number found, return 0
            return int.Parse(s[start..index]); // Extract and return the number
        }
    }

    #endregion

    #region Files Path Format

    private async void CopyFilePathToClipboard(ClipboardDataPair clipboardDataPair, string[] filePaths)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var filePath = filePaths.FirstOrDefault();
        if (File.Exists(filePath))
        {
            var exception = await RetryActionOnSTAThreadAsync(() =>
            {
                Clipboard.Clear();
                Clipboard.SetText(filePath);
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo, filePaths), StringMaxLength));
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

    #endregion

    #region File Content Format

    private async void CopyFileContentToClipboard(ClipboardDataPair clipboardDataPair, string[] filePaths)
    {
        var clipboardData = clipboardDataPair.ClipboardData;
        var filePath = filePaths.FirstOrDefault();
        if (File.Exists(filePath))
        {
            var exception = await RetryActionOnSTAThreadAsync(() =>
            {
                if (FileUtils.IsImageFile(filePath))
                {
                    Clipboard.Clear();
                    var image = filePath.ToImage();
                    Clipboard.SetImage(image);
                }
                else
                {
                    Clipboard.Clear();
                    var text = File.ReadAllText(filePath);
                    Clipboard.SetText(text);
                }
            });
            if (exception == null)
            {
                Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                    Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_to_clipboard") +
                    StringUtils.CompressString(clipboardData.GetText(CultureInfo, filePaths), StringMaxLength));
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

    #endregion

    private static async Task<Exception?> FlushClipboardAsync()
    {
        return await RetryActionOnSTAThreadAsync(Clipboard.Flush);
    }

    private static async Task<Exception?> RetryActionOnSTAThreadAsync(Action action)
    {
        for (int i = 0; i < ClipboardRetryTimes; i++)
        {
            try
            {
                await Win32Helper.StartSTATaskAsync(action);
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

    SqliteDatabase IClipboardPlus.Database => Database;

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

    #region IAsyncDisposable Interface

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
            Context.API.LogWarn(ClassName, $"Enter dispose");

            if (Database != null)
            {
                await Database.DisposeAsync();
                Database = null!;
                Context.API.LogDebug(ClassName, $"Disposed DatabaseHelper");
            }

            Context.API.LogDebug(ClassName, $"Disposed DatabaseHelper");

            ClipboardMonitor.ClipboardChanged -= OnClipboardChange;
            ClipboardMonitor.Dispose();
            ClipboardMonitor = null!;
            Context.API.LogDebug(ClassName, $"Disposed ClipboardMonitor");

            var exception = await FlushClipboardAsync();
            if (exception == null)
            {
                Context.API.LogDebug(ClassName, $"Flushed Clipboard succeeded");
            }
            else
            {
                Context.API.LogException(ClassName, $"Flushed Clipboard failed", exception);
            }

            CultureInfoChanged = null;
            Settings = null!;
            RecordsList = null!;

            Context.API.LogWarn(ClassName, $"Finish dispose");
            _disposed = true;
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    #endregion
}
