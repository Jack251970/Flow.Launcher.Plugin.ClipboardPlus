using System.Text;
using System.Text.RegularExpressions;

namespace ClipboardPlus.Core.Utils;

public static partial class StringUtils
{
    private static readonly Random _random = new();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray()
        );
    }

    public static int CountWordsCn(string s)
    {
        var nCn = (Encoding.UTF8.GetByteCount(s) - s.Length) / 2;
        return nCn;
    }

    public static int CountWordsEn(string s)
    {
        s = string.Join("", s.Where(c => c < 0x4E00));
        var collection = EnRegex().Matches(s);
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

    [GeneratedRegex("[\\S]+")]
    private static partial Regex EnRegex();
}
