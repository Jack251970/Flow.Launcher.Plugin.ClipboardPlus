using System.Text.RegularExpressions;

namespace ClipboardPlus.Core.Utils;

public static class StringUtils
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
        // Count Chinese characters in the string
        int chineseCharCount = s.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        return chineseCharCount;
    }

    public static int CountWordsEn(string s)
    {
        // Remove non-ASCII characters
        s = new string(s.Where(c => c < 128).ToArray());

        // Regex pattern to match words, including contractions
        var wordPattern = @"\b[\w'-]+\b";
        var collection = Regex.Matches(s, wordPattern);

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
}
