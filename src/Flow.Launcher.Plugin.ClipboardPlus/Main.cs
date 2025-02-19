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
    // Class name for logging
    private string ClassName => nameof(ClipboardPlus);

    #region Properties

    // Plugin context
    private PluginInitContext Context = null!;

    // State of using Windows clipboard history only
    private bool UseWindowsClipboardHistoryOnly;

    // Culture info
    private CultureInfo CultureInfo = null!;

    // Settings
    private ISettings Settings = null!;

    // Score helper
    private ScoreHelper ScoreHelper = null!;

    // Database helper
    private SqliteDatabase Database = null!;

    // Clipboard monitor instance
    // Warning: Do not init the instance in InitAsync function! This will cause issues.
    private IClipboardMonitor? ClipboardMonitor;

    // Observable data formats
    private readonly ObservableDataFormats ObservableDataFormats = new()
    {
        Images = true,
        Texts = true,
        Files = true,
        Others = false
    };

    // Windows clipboard helper
    private WindowsClipboardHelper WindowsClipboardHelper = new();

    // Records list & Score
    // Latest records are at the beginning of the list.
    private LinkedList<ClipboardDataPair> RecordsList = new();

    // Record list lock
    private readonly SemaphoreSlim RecordsLock = new(1, 1);

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

    private const int TopActionScore1 = 2 * ClipboardData.MaximumScore + 4 * ScoreInterval;
    private const int TopActionScore2 = 2 * ClipboardData.MaximumScore + 3 * ScoreInterval;
    private const int TopActionScore3 = 2 * ClipboardData.MaximumScore + 2 * ScoreInterval;
    private const int TopActionScore4 = 2 * ClipboardData.MaximumScore + 1 * ScoreInterval;

    private const int BottomActionScore1 = 8000;
    private const int BottomActionScore2 = 6000;
    private const int BottomActionScore3 = 4000;
    private const int BottomActionScore4 = 2000;

    #endregion

    #endregion

    #region Constructor

    public ClipboardPlus()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
        {
            ClipboardMonitor = new ClipboardMonitorWin()
            {
                ObserveLastEntry = false,
                ObservableFormats = ObservableDataFormats
            };
        }
        else
        {
            ClipboardMonitor = new ClipboardMonitorW()
            {
                ObserveLastEntry = false,
                ObservableFormats = ObservableDataFormats
            };
        }
    }

    #endregion

    #region IAsyncPlugin Interface

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        return Task.Run(() => Query(query));
    }

    // TODO: Remove selected count from score.
    // TODO: Add AutoCompleteText & Others properties in Record class.
    public async Task<List<Result>> Query(Query query)
    {
        var results = new List<Result>();
        if (query.FirstSearch == Settings.ClearKeyword)
        {
            // clean windows clipboard history actions
            if (WindowsClipboardHelper.IsHistoryEnabled())
            {
                results.AddRange(
                    new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_system_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_system_subtitle"),
                            IcoPath = PathHelper.AppIconPath,
                            Glyph = ResourceHelper.ClearHistoryGlyph,
                            Score = ScoreInterval6,
                            Action = (c) =>
                            {
                                _ = Task.Run(async () =>
                                {
                                    var number = await WindowsClipboardHelper.ClearAllRecordsAsync();
                                    if (number > 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_all_system_msg_subtitle"), number));
                                    }
                                    else if (number == 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                    }
                                    else
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_clear_system_msg_subtitle"));
                                    }
                                });
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_system_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_system_subtitle"),
                            IcoPath = PathHelper.AppIconPath,
                            Glyph = ResourceHelper.ClearHistoryGlyph,
                            Score = ScoreInterval5,
                            Action = (c) =>
                            {
                                _ = Task.Run(async () =>
                                {
                                    var number = await WindowsClipboardHelper.ClearUnpinnnedRecordsAsync();
                                    if (number > 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_system_msg_subtitle"), number));
                                    }
                                    else if (number == 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                    }
                                    else
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_clear_system_msg_subtitle"));
                                    }
                                });
                                return true;
                            },
                        }
                    });
            }

            // clear list and database actions
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_subtitle"),
                IcoPath = PathHelper.ListIconPath,
                Glyph = ResourceHelper.ListGlyph,
                Score = ScoreInterval4,
                Action = (c) =>
                {
                    _ = Task.Run(async () =>
                    {
                        var number = await DeleteAllRecordsFromListAsync();
                        if (number > 0)
                        {
                            Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_list_msg_subtitle"), number));
                        }
                        else
                        {
                            Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                        }
                    });
                    return true;
                },
            });
            if (!UseWindowsClipboardHistoryOnly)
            {
                results.AddRange(
                    new[]
                    {
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_subtitle"),
                            IcoPath = PathHelper.DatabaseIconPath,
                            Glyph = ResourceHelper.DatabaseGlyph,
                            Score = ScoreInterval3,
                            Action = (c) =>
                            {
                                _ = Task.Run(async () =>
                                {
                                    var number = await DeleteAllRecordsFromListDatabaseAsync();
                                    if (number > 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_both_msg_subtitle"), number));
                                    }
                                    else
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                    }
                                });
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_subtitle"),
                            IcoPath = PathHelper.UnpinIcon1Path,
                            Glyph = ResourceHelper.UnpinGlyph,
                            Score = ScoreInterval2,
                            Action = (c) =>
                            {
                                _ = Task.Run(async () =>
                                {
                                    var number = await DeleteUnpinnedRecordsFromListDatabaseAsync();
                                    if (number > 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_unpin_msg_subtitle"), number));
                                    }
                                    else
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                    }
                                });
                                return true;
                            }
                        },
                        new Result
                        {
                            Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_title"),
                            SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_subtitle"),
                            IcoPath = PathHelper.ErrorIconPath,
                            Glyph = ResourceHelper.ErrorGlyph,
                            Score = ScoreInterval1,
                            Action = (c) =>
                            {
                                _ = Task.Run(async () =>
                                {
                                    var number = await DeleteInvalidRecordsFromListDatabaseAsync();
                                    if (number > 0)
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            string.Format(Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_invalid_msg_subtitle"), number));
                                    }
                                    else
                                    {
                                        Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                                            Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_fail_msg_subtitle"));
                                    }
                                });
                                return true;
                            }
                        }
                    }
                );
            }
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
                Action = (c) =>
                {
                    _ = Win32Helper.StartSTATaskAsync(Clipboard.Clear);
                    ClipboardMonitor?.CleanClipboard();
                    return true;
                },
            });

            // connect & disconnect action
            if (ClipboardMonitor != null)
            {
                if (ClipboardMonitor.MonitorClipboard)
                {
                    results.Add(new Result
                    {
                        Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_disconnect_title"),
                        SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_disconnect_subtitle"),
                        IcoPath = PathHelper.DisconnectIconPath,
                        Glyph = ResourceHelper.DisconnectGlyph,
                        Score = Settings.ActionTop ? TopActionScore3 : BottomActionScore3,
                        Action = (c) =>
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
                        Score = Settings.ActionTop ? TopActionScore3 : BottomActionScore3,
                        Action = (c) =>
                        {
                            ClipboardMonitor.ResumeMonitoring();
                            return true;
                        },
                    });
                }
            }

            // clear action
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_title"),
                SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_clear_subtitle"),
                IcoPath = PathHelper.ClearIconPath,
                Glyph = ResourceHelper.ClearGlyph,
                Score = Settings.ActionTop ? TopActionScore4 : BottomActionScore4,
                Action = (c) =>
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
            await RecordsLock.WaitAsync();
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
            RecordsLock.Release();
            Context.API.LogDebug(ClassName, $"Added {records.Length} records successfully");
        }
        return results;
    }

    public async Task InitAsync(PluginInitContext context)
    {
        Context = context;
        ClipboardMonitor!.SetContext(context);
        WindowsClipboardHelper.SetClipboardPlus(this);

        // init path helper
        PathHelper.Init(context, GetType().Assembly.GetName().Name ?? "Flow.Launcher.Plugin.ClipboardPlus");

        // init settings
        Settings = context.API.LoadSettingJsonStorage<Settings>();
        Context.API.LogDebug(ClassName, $"Init: {Settings}");

        // init encrypt key
        StringUtils.InitEncryptKey(Settings.EncryptKey);

        // init score helper
        ScoreHelper = new ScoreHelper(ScoreInterval);

        // setup use Windows clipboard history only
        UseWindowsClipboardHistoryOnly = Settings.UseWindowsClipboardHistoryOnly && CheckUseWindowsClipboardHistoryOnly();
        Context.API.LogInfo(ClassName, $"Use Windows clipboard history only: {UseWindowsClipboardHistoryOnly}");

        if (UseWindowsClipboardHistoryOnly)
        {
            // init database
            Database = new SqliteDatabase(PathHelper.DatabasePath, this);
            await Database.InitializeDatabaseAsync();
            Context.API.LogDebug(ClassName, "Init database successfully");

            // dispose clipboard monitor
            ClipboardMonitor.ClipboardChanged -= ClipboardMonitor_OnClipboardChanged;
            ClipboardMonitor.Dispose();
            ClipboardMonitor = null;

            // init Windows clipboard helper & records from Windows clipboard history
            EnableWindowsClipboardHelper(true);
        }
        else
        {
            // init database
            var fileExists = File.Exists(PathHelper.DatabasePath);
            Database = new SqliteDatabase(PathHelper.DatabasePath, this);
            await Database.InitializeDatabaseAsync();
            Context.API.LogDebug(ClassName, "Init database successfully");

            // init records from database
            await InitRecordsFromDatabaseAndSystemAsync(fileExists, false);
            Context.API.LogDebug(ClassName, $"Init {RecordsList.Count} records successfully");

            // init & start clipboard monitor
            ClipboardMonitor.ClipboardChanged += ClipboardMonitor_OnClipboardChanged;
            ClipboardMonitor.StartMonitoring();
            if (ClipboardMonitor.GetType() == typeof(ClipboardMonitorWin))
            {
                Context.API.LogInfo(ClassName, "Init Windows clipboard monitor successfully");
            }
            else
            {
                Context.API.LogInfo(ClassName, "Init WPF clipboard monitor successfully");
            }

            // init Windows clipboard helper & records from Windows clipboard history
            if (Settings.SyncWindowsClipboardHistory)
            {
                EnableWindowsClipboardHelper(true);
            }
        }
    }

    #endregion

    #region IAsyncReloadable Interface

    public async Task ReloadDataAsync()
    {
        // reload records
        await InitRecordsFromDatabaseAndSystemAsync(!UseWindowsClipboardHistoryOnly, true);
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
                    Action = (c) =>
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
            case DataType.PlainText:
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_title"),
                    SubTitle = Context.GetTranslation("flowlauncher_plugin_clipboardplus_copy_plain_text_subtitle"),
                    IcoPath = PathHelper.CopyIconPath,
                    Glyph = ResourceHelper.CopyGlyph,
                    Score = ScoreInterval8,
                    Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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
                            Action = (c) =>
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

        var pinned = clipboardData.Pinned;
        if (!UseWindowsClipboardHistoryOnly)
        {
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
                    Action = (c) =>
                    {
                        _ = SaveToDatabaseAsync(clipboardDataPair, true);
                        return false;
                    }
                });
            }

            // Pin
            var pinStr = pinned ? "unpin" : "pin";
            results.Add(new Result
            {
                Title = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_title"),
                SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_{pinStr}_subtitle"),
                IcoPath = PathHelper.GetPinIconPath(pinned),
                Glyph = ResourceHelper.GetPinGlyph(pinned),
                Score = ScoreInterval2,
                Action = (c) =>
                {
                    _ = PinRecordAsync(clipboardDataPair, true);
                    return false;
                }
            });
        }

        if (UseWindowsClipboardHistoryOnly)
        {
            results.Add(new Result
            {
                Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_delete_title"),
                SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_delete_system_list_subtitle"),
                IcoPath = PathHelper.DeleteIconPath,
                Glyph = ResourceHelper.DeleteGlyph,
                Score = ScoreInterval1,
                Action = (c) =>
                {
                    _ = RemoveFromListDatabaseAsync(clipboardDataPair, true);
                    WindowsClipboardHelper.DeleteItemFromHistory(clipboardData);
                    return false;
                }
            });
        }
        else
        {
            // Delete
            var fromSystem = clipboardData.FromWindowsClipboardHistory();
            if (!fromSystem)
            {
                var deleteStr = pinned ? "both" : "list";
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_delete_title"),
                    SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_delete_{deleteStr}_subtitle"),
                    IcoPath = PathHelper.DeleteIconPath,
                    Glyph = ResourceHelper.DeleteGlyph,
                    Score = ScoreInterval1,
                    Action = (c) =>
                    {
                        _ = RemoveFromListDatabaseAsync(clipboardDataPair, true);
                        return false;
                    }
                });
            }
            else
            {
                var deleteStr = pinned ? "system_both" : "system_list";
                results.Add(new Result
                {
                    Title = Context.GetTranslation("flowlauncher_plugin_clipboardplus_delete_title"),
                    SubTitle = Context.GetTranslation($"flowlauncher_plugin_clipboardplus_delete_{deleteStr}_subtitle"),
                    IcoPath = PathHelper.DeleteIconPath,
                    Glyph = ResourceHelper.DeleteGlyph,
                    Score = ScoreInterval1,
                    Action = (c) =>
                    {
                        _ = RemoveFromListDatabaseAsync(clipboardDataPair, true);
                        WindowsClipboardHelper.DeleteItemFromHistory(clipboardData);
                        return false;
                    }
                });
            }
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
        // We don't need to save plugin settings because it will be called by FL.
        // Context?.API.SaveSettingJsonStorage<Settings>();
    }

    #endregion

    #region ISettingProvider Interface

    public Control CreateSettingPanel()
    {
        Context.API.LogDebug(ClassName, $"Settings Panel: {Settings}");
        return new SettingsPanel(this);
    }

    #endregion

    #region Clipboard Monitor

    private async void ClipboardMonitor_OnClipboardChanged(object? sender, ClipboardChangedEventArgs e)
    {
        Context.API.LogDebug(ClassName, "Clipboard changed");

        if (sender is not IClipboardMonitor clipboardMonitor)
        {
            return;
        }

        await RecordsLock.WaitAsync();

        // get clipboard data
        var clipboardData = GetClipboardDataItem(e.Content, e.DataType, StringUtils.GetGuid(), DateTime.Now, e.SourceApplication, clipboardMonitor.ClipboardText, clipboardMonitor.ClipboardRtfText);

        // add clipboard data
        if (!clipboardData.IsNull())
        {
            AddClipboardDataItem(clipboardData);
        }

        RecordsLock.Release();
    }

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText)
    {
        if (content is null || dataType == DataType.Other)
        {
            return ClipboardData.NULL;
        }

        // init clipboard data
        var saved = 
            UseWindowsClipboardHistoryOnly != true && 
            (Settings.KeepText && dataType == DataType.PlainText ||
            Settings.KeepText && dataType == DataType.RichText ||
            Settings.KeepImages && dataType == DataType.Image ||
            Settings.KeepFiles && dataType == DataType.Files);
        var clipboardData = new ClipboardData(content, dataType, Settings.EncryptData)
        {
            HashId = hashId,
            SenderApp = source.Name,
            InitScore = ScoreHelper.CurrentScore,
            CreateTime = createTime,
            EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
            CachedImagePath = string.Empty,
            Pinned = false,
            Saved = saved
        };

        // filter duplicate data
        if (RecordsList.Count != 0 && RecordsList.First().ClipboardData.DataEquals(clipboardData))
        {
            return ClipboardData.NULL;
        }

        // process clipboard data
        if (dataType == DataType.Image && Settings.CacheImages)
        {
            var imageName = StringUtils.FormatImageName(Settings.CacheFormat, clipboardData.CreateTime,
                string.IsNullOrEmpty(clipboardData.SenderApp) ?
                Context.GetTranslation("flowlauncher_plugin_clipboardplus_unknown_app") :
                clipboardData.SenderApp);
            var imagePath = FileUtils.SaveImageCache(clipboardData, PathHelper.ImageCachePath, imageName);
            clipboardData.CachedImagePath = imagePath;
        }
        if (dataType == DataType.RichText)
        {
            // due to some bugs, we need to convert rtf to plain text
            if (string.IsNullOrEmpty(clipboardText))
            {
                clipboardData.PlainText = StringUtils.ConvertRtfToPlainText(clipboardRtfText);
            }
            else
            {
                clipboardData.PlainText = clipboardText;
            }
        }

        return clipboardData;
    }

    private void AddClipboardDataItem(ClipboardData clipboardData)
    {
        // add to list and database if no repeat
        RecordsList.AddFirst(new ClipboardDataPair()
        {
            ClipboardData = clipboardData,
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, clipboardData))
        });

        // update score
        ScoreHelper.Add();

        // save to database if needed
        if (clipboardData.Saved)
        {
            _ = Database.AddOneRecordAsync(clipboardData, true);  // no need to wait
            Context.API.LogDebug(ClassName, $"Record {clipboardData.HashId} added to list and database");
        }
        else
        {
            Context.API.LogDebug(ClassName, $"Record {clipboardData.HashId} added to list");
        }

        // remove last record if needed
        if (RecordsList.Count >= Settings.MaxRecords)
        {
            RecordsList.Last?.Value.Dispose();
            RecordsList.RemoveLast();
        }

        // collect garbage
        GarbageCollect();
    }

    private void AddClipboardDataItem(List<ClipboardData> clipboardDataList)
    {
        var removeCount = RecordsList.Count + clipboardDataList.Count - Settings.MaxRecords;

        foreach (var clipboardData in clipboardDataList)
        {
            // add to list and database if no repeat
            RecordsList.AddFirst(new ClipboardDataPair()
            {
                ClipboardData = clipboardData,
                PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, clipboardData))
            });

            // save to database if needed
            if (clipboardData.Saved)
            {
                _ = Database.AddOneRecordAsync(clipboardData, true);  // no need to wait
                Context.API.LogDebug(ClassName, $"Record {clipboardData.HashId} added to list and database");
            }
            else
            {
                Context.API.LogDebug(ClassName, $"Record {clipboardData.HashId} added to list");
            }
        }

        // remove last record if needed
        for (int i = 0; i < removeCount; i++)
        {
            if (RecordsList.Count >= Settings.MaxRecords)
            {
                RecordsList.Last?.Value.Dispose();
                RecordsList.RemoveLast();
            }
        }

        // collect garbage
        GarbageCollect();
    }

    #endregion

    #region Windows Clipboard Helper

    public bool CheckUseWindowsClipboardHistoryOnly()
    {
        if (!WindowsClipboardHelper.IsClipboardHistorySupported())
        {
            Context.API.ShowMsg(Context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                Context.GetTranslation("flowlauncher_plugin_clipboardplus_windows_history_not_supported"));
            return false;
        }

        return true;
    }

    public async void EnableWindowsClipboardHelper(bool load)
    {
        if (UseWindowsClipboardHistoryOnly)
        {
            WindowsClipboardHelper.OnHistoryItemAdded += WindowsClipboardHelper_OnHistoryItemAdded;
        }
        WindowsClipboardHelper.OnHistoryItemRemoved += WindowsClipboardHelper_OnHistoryItemRemoved;
        WindowsClipboardHelper.OnHistoryEnabledChanged += WindowsClipboardHelper_OnHistoryEnabledChanged;
        WindowsClipboardHelper.EnableClipboardHistory();
        if (load)
        {
            await RecordsLock.WaitAsync();
            await InitRecordsFromSystemAsync(true);
            RecordsLock.Release();
        }
    }

    public async void DisableWindowsClipboardHelper(bool remove)
    {
        WindowsClipboardHelper.DisableClipboardHistory();
        if (UseWindowsClipboardHistoryOnly)
        {
            WindowsClipboardHelper.OnHistoryItemAdded -= WindowsClipboardHelper_OnHistoryItemAdded;
        }
        WindowsClipboardHelper.OnHistoryItemRemoved -= WindowsClipboardHelper_OnHistoryItemRemoved;
        WindowsClipboardHelper.OnHistoryEnabledChanged -= WindowsClipboardHelper_OnHistoryEnabledChanged;
        if (remove)
        {
            await RecordsLock.WaitAsync();
            RemoveRecordsFromSystem();
            RecordsLock.Release();
        }
    }

    private void WindowsClipboardHelper_OnHistoryItemAdded(object? sender, ClipboardData e)
    {
        AddClipboardDataItem(e);
    }

    private async void WindowsClipboardHelper_OnHistoryItemRemoved(object? sender, string[] e)
    {
        await RecordsLock.WaitAsync();
        RemoveRecordsFromSystem(r => e.Contains(r.HashId));
        RecordsLock.Release();
    }

    private async void WindowsClipboardHelper_OnHistoryEnabledChanged(object? sender, bool e)
    {
        await RecordsLock.WaitAsync();
        if (e)
        {
            await InitRecordsFromSystemAsync(true);
        }
        else
        {
            RemoveRecordsFromSystem();
        }
        RecordsLock.Release();
    }

    #endregion

    #region List & Database & Windows History

    public async Task InitRecordsFromDatabaseAndSystemAsync(bool database, bool system)
    {
        // manage database
        IEnumerable<ClipboardDataPair>? databaseDataPairs = null;
        if (database)
        {
            if (!UseWindowsClipboardHistoryOnly)
            {
                // clear expired records
                try
                {
                    foreach (var pair in Settings.KeepTimePairs)
                    {
                        Context.API.LogDebug(ClassName, $"{pair.Item1}, {pair.Item2}, {pair.Item2.ToKeepTime()}");
                        await Database.DeleteRecordsByKeepTimeAsync((int)pair.Item1, pair.Item2.ToKeepTime());
                    }
                    Context.API.LogDebug(ClassName, $"Cleared expired records successfully");
                }
                catch (Exception e)
                {
                    Context.API.LogException(ClassName, $"Cleared expired records failed", e);
                }

                // restore database records
                var records = await Database.GetAllRecordsAsync(true);
                if (records.Any())
                {
                    databaseDataPairs = records.Select(record => new ClipboardDataPair()
                    {
                        ClipboardData = record,
                        PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(this, record))
                    });
                }
            }
        }

        await RecordsLock.WaitAsync();

        // clean records
        ClearRecordsList();

        // restore database records
        if (databaseDataPairs != null)
        {
            RecordsList = new LinkedList<ClipboardDataPair>(databaseDataPairs);
        }

        // restore Windows clipboard history items
        if (system)
        {
            if (UseWindowsClipboardHistoryOnly)
            {
                await InitRecordsFromSystemAsync(false);
            }
            else if (Settings.SyncWindowsClipboardHistory)
            {
                await InitRecordsFromSystemAsync(true);
            }
        }

        RecordsLock.Release();

        // collect garbage
        GarbageCollect();

        Context.API.LogDebug(ClassName, $"Restored {RecordsList.Count} records successfully");
    }

    private async Task InitRecordsFromSystemAsync(bool check)
    {
        // get history items
        List<ClipboardData>? historyItems;
        if (check)
        {
            var latestDateTime = RecordsList.Any() ? RecordsList.Max(p => p.ClipboardData.CreateTime) : DateTime.MinValue;
            historyItems = await WindowsClipboardHelper.GetLaterHistoryItemsAsync(latestDateTime);
        }
        else
        {
            historyItems = await WindowsClipboardHelper.GetHistoryItemsAsync();
        }

        // add history items
        if (historyItems != null && historyItems.Any())
        {
            AddClipboardDataItem(historyItems);
        }
    }

    private void RemoveRecordsFromSystem(Func<ClipboardData, bool>? func = null)
    {
        List<ClipboardDataPair> recordsToRemove;
        if (func == null)
        {
            recordsToRemove = RecordsList.Where(r => r.ClipboardData.FromWindowsClipboardHistory()).ToList();
        }
        else
        {
            recordsToRemove = RecordsList.Where(r => r.ClipboardData.FromWindowsClipboardHistory() && func(r.ClipboardData)).ToList();
        }
        while (recordsToRemove.Any())
        {
            var record = recordsToRemove.First();
            RecordsList.Remove(record);
            recordsToRemove.Remove(record);
            record.Dispose();
        }
        ScoreHelper.Reset(RecordsList);
        GarbageCollect();
    }

    private async Task<int> DeleteAllRecordsFromListAsync()
    {
        await RecordsLock.WaitAsync();
        try
        {
            var number = RecordsList.Count;
            ClearRecordsList();
            ScoreHelper.Reset();
            Context.API.LogDebug(ClassName, "Deleted all records from list");
            return number;
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task<int> DeleteAllRecordsFromListDatabaseAsync()
    {
        await RecordsLock.WaitAsync();
        try
        {
            var number = RecordsList.Count;
            ClearRecordsList();
            _ = Database.DeleteAllRecordsAsync();
            ScoreHelper.Reset();
            Context.API.LogDebug(ClassName, "Deleted all records from list and database");
            return number;
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task<int> DeleteUnpinnedRecordsFromListDatabaseAsync()
    {
        await RecordsLock.WaitAsync();
        try
        {
            var unpinnedRecords = RecordsList.Where(r => !r.ClipboardData.Pinned).ToArray();
            var number = unpinnedRecords.Length;
            foreach (var record in unpinnedRecords)
            {
                record.Dispose();
                RecordsList.Remove(record);
            }
            _ = Database.DeleteUnpinnedRecordsAsync();
            ScoreHelper.Reset(RecordsList);
            Context.API.LogDebug(ClassName, "Deleted unpinned records from list and database");
            return number;
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task<int> DeleteInvalidRecordsFromListDatabaseAsync()
    {
        await RecordsLock.WaitAsync();
        try
        {
            var invalidRecords = RecordsList.Where(r => r.ClipboardData.DataToValid() is null).ToArray();
            var number = invalidRecords.Length;
            foreach (var record in invalidRecords)
            {
                record.Dispose();
                RecordsList.Remove(record);
                _ = Database.DeleteOneRecordAsync(record.ClipboardData);
            }
            ScoreHelper.Reset(RecordsList);
            Context.API.LogDebug(ClassName, "Deleted invalid records from list and database");
            return number;
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task SaveToDatabaseAsync(ClipboardDataPair clipboardDataPair, bool requery)
    {
        await RecordsLock.WaitAsync();
        try
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
                Context.API.LogDebug(ClassName, $"Save record to database: {clipboardDataPair.ClipboardData.HashId}");
            }
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task RemoveFromListAsync(ClipboardDataPair clipboardDataPair, bool requery)
    {
        await RecordsLock.WaitAsync();
        try
        {
            if (RecordsList.First?.Value == clipboardDataPair)
            {
                ScoreHelper.Subtract();
            }
            clipboardDataPair.Dispose();
            RecordsList.Remove(clipboardDataPair);
            if (requery)
            {
                ReQuery();
            }
            Context.API.LogDebug(ClassName, $"Remove record from list: {clipboardDataPair.ClipboardData.HashId}");
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task RemoveFromListDatabaseAsync(ClipboardDataPair clipboardDataPair, bool requery)
    {
        await RecordsLock.WaitAsync();
        try
        {
            if (RecordsList.First?.Value == clipboardDataPair)
            {
                ScoreHelper.Subtract();
            }
            var clipboardData = clipboardDataPair.ClipboardData;
            clipboardDataPair.Dispose();
            RecordsList.Remove(clipboardDataPair);
            if (requery)
            {
                ReQuery();
            }
            _ = Database.DeleteOneRecordAsync(clipboardData);
            Context.API.LogDebug(ClassName, $"Remove record from list and database: {clipboardDataPair.ClipboardData.HashId}");
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private async Task PinRecordAsync(ClipboardDataPair clipboardDataPair, bool requery)
    {
        await RecordsLock.WaitAsync();
        try
        {
            clipboardDataPair.TogglePinned();
            RecordsList.Remove(clipboardDataPair);
            RecordsList.AddFirst(clipboardDataPair);
            if (requery)
            {
                ReQuery();
            }
            if (!clipboardDataPair.ClipboardData.Saved)
            {
                _ = Database.AddOneRecordAsync(clipboardDataPair.ClipboardData, true);
            }
            else
            {
                _ = Database.PinOneRecordAsync(clipboardDataPair.ClipboardData);
            }
            Context.API.LogDebug(ClassName, $"Pin one record: {clipboardDataPair.ClipboardData.HashId}");
        }
        finally
        {
            RecordsLock.Release();
            GarbageCollect();
        }
    }

    private void ClearRecordsList()
    {
        foreach (var record in RecordsList)
        {
            record.Dispose();
        }
        RecordsList.Clear();
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
                Action = (c) =>
                {
                    switch (Settings.ClickAction)
                    {
                        case ClickAction.Copy:
                            CopyToClipboard(clipboardDataPair);
                            break;
                        case ClickAction.CopyPaste:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            _ = WaitWindowHideAndSimulatePaste();
                            break;
                        case ClickAction.CopyDeleteList:
                            CopyToClipboard(clipboardDataPair);
                            _ = RemoveFromListAsync(clipboardDataPair, false);
                            break;
                        case ClickAction.CopyDeleteListDatabase:
                            CopyToClipboard(clipboardDataPair);
                            _ = RemoveFromListDatabaseAsync(clipboardDataPair, false);
                            break;
                        case ClickAction.CopyPasteDeleteList:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            _ = RemoveFromListAsync(clipboardDataPair, false);
                            _ = WaitWindowHideAndSimulatePaste();
                            break;
                        case ClickAction.CopyPasteDeleteListDatabase:
                            Context.API.HideMainWindow();
                            CopyToClipboard(clipboardDataPair);
                            _ = RemoveFromListDatabaseAsync(clipboardDataPair, false);
                            _ = WaitWindowHideAndSimulatePaste();
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
            case DataType.PlainText:
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
                    case DataType.PlainText:
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
                case DataType.PlainText:
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

        var validObject = clipboardData.PlainTextToValid();
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

    bool IClipboardPlus.UseWindowsClipboardHistoryOnly => UseWindowsClipboardHistoryOnly;

    ObservableDataFormats IClipboardPlus.ObservableDataFormats => ObservableDataFormats;

    SqliteDatabase IClipboardPlus.Database => Database;

    ScoreHelper IClipboardPlus.ScoreHelper => ScoreHelper;

    ISettings IClipboardPlus.Settings => Settings;

    public ISettings LoadSettingJsonStorage() => Settings;

    public void SaveSettingJsonStorage() => Context.API.SaveSettingJsonStorage<Settings>();

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
            Context.API.LogDebug(ClassName, $"Enter dispose");

            await Database.DisposeAsync();
            Database = null!;
            Context.API.LogDebug(ClassName, $"Disposed DatabaseHelper");

            if (ClipboardMonitor != null)
            {
                ClipboardMonitor.ClipboardChanged -= ClipboardMonitor_OnClipboardChanged;
                ClipboardMonitor.Dispose();
            }
            Context.API.LogDebug(ClassName, $"Disposed ClipboardMonitor");

            DisableWindowsClipboardHelper(false);
            WindowsClipboardHelper.Dispose();
            WindowsClipboardHelper = null!;
            Context.API.LogDebug(ClassName, $"Disposed WindowsClipboardHelper");

            var exception = await FlushClipboardAsync();
            if (exception == null)
            {
                Context.API.LogDebug(ClassName, $"Flushed Clipboard succeeded");
            }
            else
            {
                Context.API.LogException(ClassName, $"Flushed Clipboard failed", exception);
            }

            ClearRecordsList();
            RecordsList = null!;
            RecordsLock.Dispose();

            Context.API.LogDebug(ClassName, $"Disposed RecordsList");

            CultureInfoChanged = null;
            Settings = null!;

            Context.API.LogDebug(ClassName, $"Finish dispose");
            _disposed = true;
        }
    }

    #endregion

    #region Garbage Collect

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    #endregion
}
