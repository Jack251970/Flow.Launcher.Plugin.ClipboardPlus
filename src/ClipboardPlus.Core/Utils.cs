using System.Text;
using System.Text.RegularExpressions;

namespace ClipboardPlus.Core;

public static partial class Utils
{
    private static readonly Random _random = new();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray()
        );
    }

    public static int CountWords(string s)
    {
        return CountWordsCn(s) + CountWordsEn(s);
    }

    public static int CountWordsCn(string s)
    {
        var nCn = (Encoding.UTF8.GetByteCount(s) - s.Length) / 2;
        // var nCn = s.ToCharArray().Count(c => c >= 0x4E00 && c <= 0x9FFF);
        return nCn;
    }

    public static int CountWordsEn(string s)
    {
        // TODO: count more reasonable
        s = string.Join("", s.Where(c => c < 0x4E00));
        var collection = MyRegex().Matches(s);
        return collection.Count;
    }

    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("D");
    }

    public static string FormatImageName(string format, DateTime dateTime, string appname = "")
    {
        if (format.Contains("{app}"))
        {
            format = format.Replace("{app}", "");
        }
        else
        {
            appname = "";
        }
        var imageName = dateTime.ToString(format) + appname;
        return imageName;
    }
    
    public static string? SaveImageCache(ClipboardData clipboardData, DirectoryInfo clipCacheDir, string? name=null)
    {
        if (clipboardData.Data is not Image img)
        {
            return null;
        }

        name = string.IsNullOrWhiteSpace(name) ? RandomString(10): name;
        var path = Path.Join(clipCacheDir.FullName, $"{name}.png");

        img.Save(path);
        return path;
    }

    [GeneratedRegex("[\\S]+")]
    private static partial Regex MyRegex();
}

public static class Retry
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

    public static T Do<T>(Func<T> action, int retryInterval, int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (attempted > 0)
                    Thread.Sleep(retryInterval);
                return action();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }
}

public static class CmBoxIndexMapper
{
    // ComBox index to keep time mapper 
    private static readonly Dictionary<KeepTime, int> KeepTimeDict =
        new(
            new List<KeyValuePair<KeepTime, int>>()
            {
                new(KeepTime.Always, int.MaxValue),
                new(KeepTime.Hour1, 1),
                new(KeepTime.Hours12, 12),
                new(KeepTime.Hours24, 24),
                new(KeepTime.Days3, 72),
                new(KeepTime.Days7, 168),
                new(KeepTime.Month1, 720),
                new(KeepTime.Months6, 4320),
                new(KeepTime.Year1, 8640),
            }
        );

    public static int ToKeepTime(KeepTime idx)
    {
        var k = KeepTimeDict.ContainsKey(idx) ? idx : 0;
        return KeepTimeDict[k];
    }
}
