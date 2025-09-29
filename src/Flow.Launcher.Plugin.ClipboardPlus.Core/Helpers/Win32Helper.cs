using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

/// <summary>
/// Provides static helper for Win32.
/// Codes are edited from: https://github.com/files-community/Files.
/// </summary>
public class Win32Helper
{
    [SupportedOSPlatform("windows5.0")]
    public static Task StartSTATaskAsync(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        Thread thread = new(() =>
        {
            PInvoke.OleInitialize();

            try
            {
                action();
                taskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
            finally
            {
                PInvoke.OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    [SupportedOSPlatform("windows5.0")]
    public static Task StartSTATaskAsync(Func<Task> func)
    {
        var taskCompletionSource = new TaskCompletionSource();
        Thread thread = new(async () =>
        {
            PInvoke.OleInitialize();

            try
            {
                await func();
                taskCompletionSource.SetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
            finally
            {
                PInvoke.OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    [SupportedOSPlatform("windows5.0")]
    public static Task<T?> StartSTATaskAsync<T>(Func<T> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T?>();

        Thread thread = new(() =>
        {
            PInvoke.OleInitialize();

            try
            {
                taskCompletionSource.SetResult(func());
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
            finally
            {
                PInvoke.OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }

    [SupportedOSPlatform("windows5.0")]
    public static Task<T?> StartSTATaskAsync<T>(Func<Task<T>> func)
    {
        var taskCompletionSource = new TaskCompletionSource<T?>();

        Thread thread = new(async () =>
        {
            PInvoke.OleInitialize();
            try
            {
                taskCompletionSource.SetResult(await func());
            }
            catch (Exception e)
            {
                taskCompletionSource.SetException(e);
            }
            finally
            {
                PInvoke.OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return taskCompletionSource.Task;
    }
}
