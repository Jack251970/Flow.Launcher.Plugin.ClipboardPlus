﻿using System.Text;
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

    public static int CountWords(string s)
    {
        return CountWordsEn(s) + CountWordsCn(s);
    }

    public static int CountWordsEn(string s)
    {
        s = string.Join("", s.Where(c => c < 0x4E00));
        var collection = EnRegex().Matches(s);
        return collection.Count;
    }

    public static int CountWordsCn(string s)
    {
        var nCn = (Encoding.UTF8.GetByteCount(s) - s.Length) / 2;
        return nCn;
    }

    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("D");
    }

    public static string FormatImageName(string format, DateTime dateTime, string appname = "unknown")
    {
        var imageName = format
            .Replace("yyyy", dateTime.ToString("yyyy"))
            .Replace("MM", dateTime.ToString("MM"))
            .Replace("dd", dateTime.ToString("dd"))
            .Replace("hh", dateTime.ToString("hh"))
            .Replace("mm", dateTime.ToString("mm"))
            .Replace("ss", dateTime.ToString("ss"))
            .Replace("app", appname);
        return imageName;
    }

    [GeneratedRegex("[\\S]+")]
    private static partial Regex EnRegex();
}
