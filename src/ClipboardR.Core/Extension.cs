﻿using System.Drawing.Imaging;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace ClipboardR.Core;

public static class ClipImageExtension
{
    public static BitmapImage ToBitmapImage(this Image img)
    {
        var stream = new MemoryStream();
        img.Save(stream, ImageFormat.Png);
        var im = new BitmapImage();
        im.BeginInit();
        im.CacheOption = BitmapCacheOption.OnLoad;
        im.StreamSource = stream;
        im.EndInit();
        im.Freeze();
        stream.Close();
        return im;
    }

    public static string ToBase64(this Image img)
    {
        using var m = new MemoryStream();
        img.Save(m, img.RawFormat);
        return Convert.ToBase64String(m.ToArray());
    }

    public static string ToBase64(this BitmapImage image)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(image);
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static BitmapImage ToBitmapImage(this string b64)
    {
        return b64.ToImage().ToBitmapImage();
    }

    public static Image ToImage(this string b64)
    {
        byte[] bytes = Convert.FromBase64String(b64);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

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

    // public static string ToBase64(object o)
    // {
    //     using var m = new MemoryStream();
    //     var bf = new BinaryFormatter();
    //     bf.Serialize();
    //     return Convert.ToBase64String(m.ToArray());
    // }
}