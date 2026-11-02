// ImagePinTests.cs
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class ImagePinTests
{
    [Fact]
    public void FrozenImageTogglePreservesHashIdentityAndRecordMetadata()
    {
        var image = MakeImage();
        image.Freeze();
        var original = Record(image);
        var pair = new ClipboardDataPair
        {
            ClipboardData = original,
            PreviewPanel = new Lazy<UserControl>(() => throw new InvalidOperationException("Preview must stay lazy"))
        };

        pair.TogglePinned();
        AssertRecord(original, pair.ClipboardData, true);
        pair.TogglePinned();
        AssertRecord(original, pair.ClipboardData, false);
        Assert.False(pair.PreviewPanel.IsValueCreated);
    }

    [Fact]
    public void MutableImageIsRehashedEvenIfFrozenAfterItChanges()
    {
        var image = MakeImage();
        var original = Record(image);
        image.WritePixels(new System.Windows.Int32Rect(0, 0, 1, 1), new byte[] { 40, 50, 60, 255 }, 4, 0);
        image.Freeze();
        var cloned = original.Clone(true);
        var expected = Record(image);

        Assert.NotEqual(original.DataMd5, expected.DataMd5);
        Assert.Equal(expected.DataMd5, cloned.DataMd5);
        Assert.True(cloned.Pinned);
    }

    [Fact]
    public void CloneWithNewIdentityStillRecomputesTheImageHash()
    {
        var image = MakeImage();
        var original = Record(image);
        image.WritePixels(new System.Windows.Int32Rect(0, 0, 1, 1), new byte[] { 80, 90, 100, 255 }, 4, 0);
        var cloned = original.Clone();
        var expected = Record(image);

        Assert.NotEqual(original.HashId, cloned.HashId);
        Assert.Equal(expected.DataMd5, cloned.DataMd5);
    }

    private static WriteableBitmap MakeImage() => new(1, 1, 96, 96, PixelFormats.Bgra32, null);

    private static ClipboardData Record(BitmapSource image) => new(image, DataType.Image, false)
    {
        HashId = "synthetic",
        SenderApp = "synthetic.exe",
        InitScore = 123,
        CreateTime = new DateTime(2025, 1, 1),
        EncryptKeyMd5 = "synthetic-key",
        CachedImagePath = "synthetic-path",
        Pinned = false,
        Saved = true,
        PlainText = "synthetic-text"
    };

    private static void AssertRecord(ClipboardData expected, ClipboardData actual, bool pinned)
    {
        Assert.Equal(pinned, actual.Pinned);
        Assert.Same(expected.Data, actual.Data);
        Assert.Equal(expected.DataMd5, actual.DataMd5);
        Assert.Equal(expected.HashId, actual.HashId);
        Assert.Equal(expected.DataType, actual.DataType);
        Assert.Equal(expected.SenderApp, actual.SenderApp);
        Assert.Equal(expected.InitScore, actual.InitScore);
        Assert.Equal(expected.CreateTime, actual.CreateTime);
        Assert.Equal(expected.EncryptKeyMd5, actual.EncryptKeyMd5);
        Assert.Equal(expected.CachedImagePath, actual.CachedImagePath);
        Assert.Equal(expected.Saved, actual.Saved);
        Assert.Equal(expected.PlainText, actual.PlainText);
        Assert.Equal(expected.EncryptData, actual.EncryptData);
    }
}
