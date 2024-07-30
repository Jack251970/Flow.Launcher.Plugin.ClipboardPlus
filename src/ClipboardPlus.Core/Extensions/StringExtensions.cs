using System.Security.Cryptography;
using System.Text;

namespace ClipboardPlus.Core.Extensions;

public static class StringExtension
{
    public static string GetMd5(this string s)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(s);
        byte[] hash = MD5.HashData(inputBytes);
        var hex = hash.Select(i => i.ToString("X2"));
        return string.Join("", hex);
    }

    public static string GetSha256(this string s)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(s);
        var hash = SHA256.HashData(inputBytes);
        var hex = hash.Select(i => i.ToString("X2"));
        return string.Join("", hex);
    }
}
