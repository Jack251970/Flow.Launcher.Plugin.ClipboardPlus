using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using DataFormats = System.Windows.DataFormats;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static partial class StringUtils
{
    #region Random String

    private static readonly Random _random = new();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray()
        );
    }

    #endregion

    #region Count Words

    public static int CountWords(string s)
    {
        return CountWordsEn(s) + CountWordsCn(s);
    }

    public static int CountWordsEn(string s)
    {
        s = string.Join(string.Empty, s.Where(c => c < 0x4E00));
        var collection = EnRegex().Matches(s);
        return collection.Count;
    }

    public static int CountWordsCn(string s)
    {
        var nCn = (Encoding.UTF8.GetByteCount(s) - s.Length) / 2;
        return nCn;
    }

    [GeneratedRegex("[\\S]+")]
    private static partial Regex EnRegex();

    #endregion

    #region Guid String

    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("D");
    }

    #endregion

    #region Format String

    public static string FormatImageName(string format, DateTime dateTime, string appname = "Flow.Launcher.exe")
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

    #endregion

    #region Length Compression

    public static string CompressString(string str, int en_length)
    {
        var currentLength = 0;
        var i = 0;
        var ellipsisLength = 3;
        var restLength = en_length - ellipsisLength;

        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        if (CountStringEnLength(str) <= en_length)
        {
            return str;
        }

        for (; i < str.Length; i++)
        {
            if (IsWideCharacter(str[i]))
            {
                currentLength += 2;
            }
            else
            {
                currentLength += 1;
            }

            if (currentLength > restLength)
            {
                break;
            }
        }

        return string.Concat(str.AsSpan(0, i), "...");
    }

    private static int CountStringEnLength(string str)
    {
        var enLength = 0;
        for (var i = 0; i < str.Length; i++)
        {
            enLength += IsWideCharacter(str[i]) ? 2 : 1;
        }
        return enLength;
    }

    private static bool IsWideCharacter(char ch)
    {
        return (ch >= 0x4E00 && ch <= 0x9FFF) ||// CJK Unified Ideographs
            (ch >= 0x3400 && ch <= 0x4DBF) ||   // CJK Unified Ideographs Extension A
            (ch >= 0x20000 && ch <= 0x2A6DF) || // CJK Unified Ideographs Extension B
            (ch >= 0x2A700 && ch <= 0x2B73F) || // CJK Unified Ideographs Extension C
            (ch >= 0x2B740 && ch <= 0x2B81F) || // CJK Unified Ideographs Extension D
            (ch >= 0x2B820 && ch <= 0x2CEAF) || // CJK Unified Ideographs Extension E
            (ch >= 0xF900 && ch <= 0xFAFF) ||   // CJK Compatibility Ideographs
            (ch >= 0x2F800 && ch <= 0x2FA1F) || // CJK Compatibility Ideographs Supplement
            (ch >= 0xFF00 && ch <= 0xFFEF) ||   // Full-width punctuation and half-width Katakana
            (ch >= 0x2E80 && ch <= 0x2EFF) ||   // CJK Radicals Supplement
            (ch >= 0x2F00 && ch <= 0x2FDF) ||   // Kangxi Radicals
            (ch >= 0x2FF0 && ch <= 0x2FFF) ||   // Ideographic Description Characters
            (ch >= 0x3000 && ch <= 0x303F) ||   // CJK Symbols and Punctuation
            (ch >= 0x3040 && ch <= 0x309F) ||   // Hiragana
            (ch >= 0x30A0 && ch <= 0x30FF) ||   // Katakana
            (ch >= 0x3100 && ch <= 0x312F) ||   // Bopomofo
            (ch >= 0x3130 && ch <= 0x318F) ||   // Hangul Compatibility Jamo
            (ch >= 0x3200 && ch <= 0x32FF) ||   // Enclosed CJK Letters and Months
            (ch >= 0xFE30 && ch <= 0xFE4F) ||   // CJK Compatibility Forms
            (ch >= 0xFE50 && ch <= 0xFE6F) ||   // Small Form Variants
            (ch >= 0xAC00 && ch <= 0xD7AF) ||   // Hangul Syllables
            (ch >= 0x1100 && ch <= 0x11FF) ||   // Hangul Jamo
            (ch >= 0x1F300 && ch <= 0x1F5FF) || // Miscellaneous Symbols and Pictographs (includes most emojis)
            (ch >= 0x1F600 && ch <= 0x1F64F) || // Emoticons
            (ch >= 0x1F680 && ch <= 0x1F6FF) || // Transport and Map Symbols
            (ch >= 0x1F700 && ch <= 0x1F77F);   // Alchemical Symbols
    }

    #endregion

    #region Hash String

    public static string GetMd5(string s)
    {
        s ??= string.Empty;
        var inputBytes = Encoding.UTF8.GetBytes(s);
        var hash = MD5.HashData(inputBytes);
        var hex = hash.Select(i => i.ToString("X2"));
        return string.Join(string.Empty, hex);
    }

    public static string GetSha256(string s)
    {
        s ??= string.Empty;
        var inputBytes = Encoding.UTF8.GetBytes(s);
        var hash = SHA256.HashData(inputBytes);
        var hex = hash.Select(i => i.ToString("X2"));
        return string.Join(string.Empty, hex);
    }

    public static bool IsMd5(string s)
    {
        var md5Regex = Md5Regex();
        return md5Regex.IsMatch(s);
    }

    [GeneratedRegex("^[a-fA-F0-9]+$")]
    internal static partial Regex Md5Regex();

    #endregion

    #region Encrypt & Decrypt

    private static string encryptKey = string.Empty;
    public static string EncryptKey => encryptKey;

    private static string encryptKeyMd5 = string.Empty;
    public static string EncryptKeyMd5 => encryptKeyMd5;

    private static bool initialized = false;

    /// <summary>
    /// Initialize the AES key for encryption and decryption
    /// </summary>
    /// <param name="key">
    /// The key to encrypt and decrypt the string, length must be 32
    /// </param>
    public static void InitEncryptKey(string key)
    {
        if (!initialized)
        {
            encryptKey = key;
            encryptKeyMd5 = GetMd5(key);
            initialized = true;
        }
    }

    /// <summary>
    /// Generate a random AES key
    /// </summary>
    /// <returns>
    /// The AES key with length 32
    /// </returns>
    public static string GenerateEncryptKey()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Encrypt a string using AES
    /// </summary>
    /// <param name="s">
    /// The string to encrypt, length can be any
    /// </param>
    /// <param name="key">
    /// The key to encrypt the string, length must be 32
    /// </param>
    /// <returns>
    /// The encrypted string
    /// </returns>
    public static string Encrypt(string s, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var iv = new byte[16]; // AES block size is 16 bytes
        var inputBytes = Encoding.UTF8.GetBytes(s);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(inputBytes, 0, inputBytes.Length);
        cs.FlushFinalBlock();
        var encryptedBytes = ms.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypt a string using AES
    /// </summary>
    /// <param name="s">
    /// The string to decrypt, length must be multiple of 4
    /// </param>
    /// <param name="key">
    /// The key to decrypt the string, length must be 32
    /// </param>
    /// <returns>
    /// The decrypted string
    /// </returns>
    public static string Decrypt(string s, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var iv = new byte[16]; // AES block size is 16 bytes
        var inputBytes = Convert.FromBase64String(s);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = iv;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(inputBytes, 0, inputBytes.Length);
        cs.FlushFinalBlock();
        var decryptedBytes = ms.ToArray();
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    #endregion

    #region Rich Text

    public static string ConvertRtfToPlainText(string rtfString)
    {
        try
        {
            // Create a RichTextBox
            var richTextBox = new RichTextBox();

            // Load the RTF string into the RichTextBox
            var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

            using (var stream = new MemoryStream(Encoding.Default.GetBytes(rtfString)))
            {
                range.Load(stream, DataFormats.Rtf);
            }

            // Extract and return the plain text
            return range.Text.Trim();
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    #endregion
}
