using Windows.Win32;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

/// <summary>
/// Provides static helper for Win32.
/// Codes are edited from: https://github.com/files-community/Files.
/// </summary>
public class Win32Helper
{
    public static Task StartSTATask(Action action)
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
            catch (Exception)
            {
                taskCompletionSource.SetResult();
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
