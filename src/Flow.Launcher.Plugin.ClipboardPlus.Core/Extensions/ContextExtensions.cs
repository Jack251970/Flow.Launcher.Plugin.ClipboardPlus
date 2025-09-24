// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

using Flow.Launcher.Plugin.SharedModels;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Extensions;

/// <summary>
/// Extension methods for PluginInitContext to improve RELEASE performance and add DEBUG log
/// </summary>
public static class ContextExtensions
{
    public static void RestartApp(this PluginInitContext? context)
    {
#if DEBUG
        context?.API.RestartApp();
#else
        context!.API.RestartApp();
#endif
    }

    public static void ShowMsgError(this PluginInitContext? context, string title, string subTitle = "")
    {
#if DEBUG
        context?.API.ShowMsgError(title, subTitle);
#else
        context!.API.ShowMsgError(title, subTitle);
#endif
    }

    public static void ShowMsg(this PluginInitContext? context, string title, string subTitle = "", string iconPath = "")
    {
#if DEBUG
        context?.API.ShowMsg(title, subTitle, iconPath);
#else
        context!.API.ShowMsg(title, subTitle, iconPath);
#endif
    }

    public static void ShowMsg(this PluginInitContext? context, string title, string subTitle, string iconPath, bool useMainWindowAsOwner = true)
    {
#if DEBUG
        context?.API.ShowMsg(title, subTitle, iconPath, useMainWindowAsOwner);
#else
        context!.API.ShowMsg(title, subTitle, iconPath, useMainWindowAsOwner);
#endif
    }

    public static string GetTranslation(this PluginInitContext? context, string key)
    {
#if DEBUG
        return context?.API.GetTranslation(key) ?? key;
#else
        return context!.API.GetTranslation(key);
#endif
    }

    public static void LogDebug(this PluginInitContext? context, string className, string message, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        context?.API.LogDebug(className, message, methodName);
        Debug.WriteLine($"Debug|{className}|{methodName}|{message}");
#else
        context!.API.LogDebug(className, message, methodName);
#endif
    }

    public static void LogInfo(this PluginInitContext? context, string className, string message, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        context?.API.LogInfo(className, message, methodName);
        Debug.WriteLine($"Info|{className}|{methodName}|{message}");
#else
        context!.API.LogInfo(className, message, methodName);
#endif
    }

    public static void LogWarn(this PluginInitContext? context, string className, string message, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        context?.API.LogWarn(className, message, methodName);
        Debug.WriteLine($"Warn|{className}|{methodName}|{message}");
#else
        context!.API.LogWarn(className, message, methodName);
#endif
    }

    public static void LogException(this PluginInitContext? context, string className, string message, Exception e, [CallerMemberName] string methodName = "")
    {
#if DEBUG
        context?.API.LogException(className, message, e, methodName);
        Debug.WriteLine($"Exception|{className}|{methodName}|{message}|{e}");
        Debugger.Break();
        context?.API.LogException(className, message, e, methodName);
#else
        context!.API.LogException(className, message, e, methodName);
        context!.ShowMsgError(context.GetTranslation("flowlauncher_plugin_clipboardplus_exception_title"), context.GetTranslation("flowlauncher_plugin_clipboardplus_exception_subtitle"));
#endif
    }

    public static void OpenDirectory(this PluginInitContext? context, string DirectoryPath, string? FileNameOrFilePath = null)
    {
#if DEBUG
        context?.API.OpenDirectory(DirectoryPath, FileNameOrFilePath);
#else
        context!.API.OpenDirectory(DirectoryPath, FileNameOrFilePath);
#endif
    }

    public static void OpenAppUri(this PluginInitContext? context, string appUri)
    {
#if DEBUG
        context?.API.OpenAppUri(appUri);
#else
        context!.API.OpenAppUri(appUri);
#endif
    }

    public static void ReQuery(this PluginInitContext? context, bool reselect = true)
    {
#if DEBUG
        context?.API.ReQuery(reselect);
#else
        context!.API.ReQuery(reselect);
#endif
    }

    public static void RegisterGlobalKeyboardCallback(this PluginInitContext? context, Func<int, int, SpecialKeyState, bool> callback)
    {
#if DEBUG
        context?.API.RegisterGlobalKeyboardCallback(callback);
#else
        context!.API.RegisterGlobalKeyboardCallback(callback);
#endif
    }

    public static void RemoveGlobalKeyboardCallback(this PluginInitContext? context, Func<int, int, SpecialKeyState, bool> callback)
    {
#if DEBUG
        context?.API.RemoveGlobalKeyboardCallback(callback);
#else
        context!.API.RemoveGlobalKeyboardCallback(callback);
#endif
    }

    public static bool Disabled(this PluginInitContext? context)
    {
#if DEBUG
        return context?.CurrentPluginMetadata.Disabled ?? false;
#else
        return context!.CurrentPluginMetadata.Disabled;
#endif
    }

    public static MessageBoxResult ShowMsgBox(this PluginInitContext? context, string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.OK)
    {
#if DEBUG
        return context?.API.ShowMsgBox(messageBoxText, caption, button, icon, defaultResult) ?? MessageBoxResult.OK;
#else
        return context!.API.ShowMsgBox(messageBoxText, caption, button, icon, defaultResult);
#endif
    }

    public static MatchResult FuzzySearch(this PluginInitContext? context, string query, string stringToCompare)
    {
#if DEBUG
        return context?.API.FuzzySearch(query, stringToCompare) ?? new MatchResult(true, SearchPrecisionScore.Regular);
#else
        return context!.API.FuzzySearch(query, stringToCompare);
#endif
    }
}
