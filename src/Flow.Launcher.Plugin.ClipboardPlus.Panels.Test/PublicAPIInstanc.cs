using Flow.Launcher.Plugin.SharedModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class PublicAPIInstance : IPublicAPI
{
    private static readonly string ClassName = nameof(PublicAPIInstance);

    public event VisibilityChangedEventHandler VisibilityChanged = null!;
    public event ActualApplicationThemeChangedEventHandler ActualApplicationThemeChanged = null!;

    public void RegisterGlobalKeyboardCallback(Func<int, int, SpecialKeyState, bool> callback)
    {
        throw new NotImplementedException();
    }

    public void RemoveGlobalKeyboardCallback(Func<int, int, SpecialKeyState, bool> callback)
    {
        throw new NotImplementedException();
    }

    public Task HttpDownloadAsync(string url, string filePath, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void ReQuery(bool reselect = true)
    {
        throw new NotImplementedException();
    }

    public void ChangeQuery(string query, bool requery = false)
    {
        throw new NotImplementedException();
    }

    public void RestartApp()
    {
        throw new NotImplementedException();
    }

    public void ShellRun(string cmd, string filename = "cmd.exe")
    {
        throw new NotImplementedException();
    }

    public void CopyToClipboard(string text, bool directCopy = false, bool showDefaultNotification = true)
    {
        throw new NotImplementedException();
    }

    public void SaveAppAllSettings()
    {
        throw new NotImplementedException();
    }

    public Task ReloadAllPluginData()
    {
        throw new NotImplementedException();
    }

    public void CheckForNewUpdate()
    {
        throw new NotImplementedException();
    }

    public void ShowMsgError(string title, string subTitle = "")
    {
        throw new NotImplementedException();
    }

    public void ShowMainWindow()
    {
        throw new NotImplementedException();
    }

    public void HideMainWindow()
    {
        throw new NotImplementedException();
    }

    public bool IsMainWindowVisible()
    {
        throw new NotImplementedException();
    }

    public void ShowMsg(string title, string subTitle = "", string iconPath = "")
    {
        throw new NotImplementedException();
    }

    public void ShowMsg(string title, string subTitle, string iconPath, bool useMainWindowAsOwner = true)
    {
        throw new NotImplementedException();
    }

    public void OpenSettingDialog()
    {
        throw new NotImplementedException();
    }

    public string GetTranslation(string key)
    {
        var translation = Application.Current.TryFindResource(key);
        if (translation is string)
        {
            return translation.ToString()!;
        }
        else
        {
            LogError(ClassName, $"No Translation for key {key}");
            return $"No Translation for key {key}";
        }
    }

    public List<PluginPair> GetAllPlugins()
    {
        throw new NotImplementedException();
    }

    public MatchResult FuzzySearch(string query, string stringToCompare)
    {
        throw new NotImplementedException();
    }

    public Task<string> HttpGetStringAsync(string url, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> HttpGetStreamAsync(string url, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void AddActionKeyword(string pluginId, string newActionKeyword)
    {
        throw new NotImplementedException();
    }

    public void RemoveActionKeyword(string pluginId, string oldActionKeyword)
    {
        throw new NotImplementedException();
    }

    public bool ActionKeywordAssigned(string actionKeyword)
    {
        throw new NotImplementedException();
    }

    public void SavePluginSettings()
    {
        throw new NotImplementedException();
    }

    public void LogDebug(string className, string message, [CallerMemberName] string methodName = "")
    {
        Debug.WriteLine($"DEBUG: [{className}.{methodName}] {message}");
    }

    public void LogInfo(string className, string message, [CallerMemberName] string methodName = "")
    {
        Debug.WriteLine($"INFO: [{className}.{methodName}] {message}");
    }

    public void LogWarn(string className, string message, [CallerMemberName] string methodName = "")
    {
        Debug.WriteLine($"WARN: [{className}.{methodName}] {message}");
    }

    public void LogError(string className, string message, [CallerMemberName] string methodName = "")
    {
        Debug.WriteLine($"ERROR: [{className}.{methodName}] {message}");
    }

    public void LogException(string className, string message, Exception e, [CallerMemberName] string methodName = "")
    {
        Debug.WriteLine($"EXCEPTION: [{className}.{methodName}] {message}\n{e}");
    }

    public T LoadSettingJsonStorage<T>() where T : new()
    {
        return new T();
    }

    public void SaveSettingJsonStorage<T>() where T : new()
    {
        throw new NotImplementedException();
    }

    public void OpenDirectory(string DirectoryPath, string FileNameOrFilePath = null!)
    {
        throw new NotImplementedException();
    }

    public void OpenUrl(Uri url, bool? inPrivate = null)
    {
        throw new NotImplementedException();
    }

    public void OpenUrl(string url, bool? inPrivate = null)
    {
        throw new NotImplementedException();
    }

    public void OpenAppUri(Uri appUri)
    {
        throw new NotImplementedException();
    }

    public void OpenAppUri(string appUri)
    {
        throw new NotImplementedException();
    }

    public void ToggleGameMode()
    {
        throw new NotImplementedException();
    }

    public void SetGameMode(bool value)
    {
        throw new NotImplementedException();
    }

    public bool IsGameModeOn()
    {
        throw new NotImplementedException();
    }

    public void FocusQueryTextBox()
    {
        throw new NotImplementedException();
    }

    public Task HttpDownloadAsync(string url, string filePath, Action<double> reportProgress, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public void BackToQueryResults()
    {
        throw new NotImplementedException();
    }

    public MessageBoxResult ShowMsgBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
        throw new NotImplementedException();
    }

    public Task ShowProgressBoxAsync(string caption, Func<Action<double>, Task> reportProgressAsync, Action cancelProgress)
    {
        throw new NotImplementedException();
    }

    public void StartLoadingBar()
    {
        throw new NotImplementedException();
    }

    public void StopLoadingBar()
    {
        throw new NotImplementedException();
    }

    public List<ThemeData> GetAvailableThemes()
    {
        throw new NotImplementedException();
    }

    public ThemeData GetCurrentTheme()
    {
        throw new NotImplementedException();
    }

    public bool SetCurrentTheme(ThemeData theme)
    {
        throw new NotImplementedException();
    }

    public void SavePluginCaches()
    {
        throw new NotImplementedException();
    }

    public Task<T> LoadCacheBinaryStorageAsync<T>(string cacheName, string cacheDirectory, T defaultData) where T : new()
    {
        throw new NotImplementedException();
    }

    public Task SaveCacheBinaryStorageAsync<T>(string cacheName, string cacheDirectory) where T : new()
    {
        throw new NotImplementedException();
    }

    public ValueTask<ImageSource> LoadImageAsync(string path, bool loadFullImage, bool cacheImage)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdatePluginManifestAsync(bool usePrimaryUrlOnly, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<UserPlugin> GetPluginManifest()
    {
        throw new NotImplementedException();
    }

    public bool PluginModified(string id)
    {
        throw new NotImplementedException();
    }

    public long StopwatchLogDebug(string className, string message, Action action, string methodName)
    {
        throw new NotImplementedException();
    }

    public Task<long> StopwatchLogDebugAsync(string className, string message, Func<Task> action, string methodName)
    {
        throw new NotImplementedException();
    }

    public long StopwatchLogInfo(string className, string message, Action action, string methodName)
    {
        throw new NotImplementedException();
    }

    public Task<long> StopwatchLogInfoAsync(string className, string message, Func<Task> action, string methodName)
    {
        throw new NotImplementedException();
    }

    public void ShowMsgErrorWithButton(string title, string buttonText, Action buttonAction, string subTitle = "")
    {
        throw new NotImplementedException();
    }

    public void ShowMsgWithButton(string title, string buttonText, Action buttonAction, string subTitle = "", string iconPath = "")
    {
        throw new NotImplementedException();
    }

    public void ShowMsgWithButton(string title, string buttonText, Action buttonAction, string subTitle, string iconPath, bool useMainWindowAsOwner = true)
    {
        throw new NotImplementedException();
    }

    public void OpenWebUrl(Uri url, bool? inPrivate = null)
    {
        throw new NotImplementedException();
    }

    public void OpenWebUrl(string url, bool? inPrivate = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdatePluginAsync(PluginMetadata pluginMetadata, UserPlugin plugin, string zipFilePath)
    {
        throw new NotImplementedException();
    }

    public bool InstallPlugin(UserPlugin plugin, string zipFilePath)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UninstallPluginAsync(PluginMetadata pluginMetadata, bool removePluginSettings)
    {
        throw new NotImplementedException();
    }

    public bool IsApplicationDarkTheme()
    {
        throw new NotImplementedException();
    }

    public string GetDataDirectory()
    {
        throw new NotImplementedException();
    }

    public string GetLogDirectory()
    {
        throw new NotImplementedException();
    }

    public List<PluginPair> GetAllInitializedPlugins(bool includeFailed)
    {
        throw new NotImplementedException();
    }
}
