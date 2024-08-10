namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static class TaskUtils
{
    public static void Do(Action action, int retryInterval, int maxAttemptCount = 3)
    {
        Do<object>(
            () =>
            {
                action();
                return null!;
            },
            retryInterval,
            maxAttemptCount
        );
    }

    public static void SafeDo(Action action, int retryInterval, int maxAttemptCount = 3, PluginInitContext? context = null, string className = "")
    {
        try
        {
            Do(action, retryInterval, maxAttemptCount);
        }
        catch (Exception ex)
        {
            context?.API.LogException(className, "Failed to execute action", ex);
        }
    }

    public static T Do<T>(Func<T> action, int retryInterval, int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (attempted > 0)
                {
                    Thread.Sleep(retryInterval);
                }
                return action();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }

    public static T SafeDo<T>(Func<T> action, int retryInterval, int maxAttemptCount = 3, PluginInitContext? context = null, string className = "")
    {
        try
        {
            return Do(action, retryInterval, maxAttemptCount);
        }
        catch (Exception ex)
        {
            context?.API.LogException(className, "Failed to execute action", ex);
            return default!;
        }
    }
}
