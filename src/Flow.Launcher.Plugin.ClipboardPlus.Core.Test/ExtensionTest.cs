using System.Drawing;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class ExtensionTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    public ExtensionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(@"Images\clipboard.png")]
    public void TestToBitmapImage(string filename)
    {
        var img = new Bitmap(filename);
        _testOutputHelper.WriteLine(img.RawFormat.ToString());
        var im = img.ToBitmapImage();
        _testOutputHelper.WriteLine(im.Format.ToString());
        var bm = new BitmapImage(new Uri(Path.Combine(_baseDirectory, filename), UriKind.Absolute));
        _testOutputHelper.WriteLine(bm.Format.ToString());
    }

    [Fact]
    public void TestImageToBase64()
    {
        using var f = File.OpenText(@"Images\clipboard_b64.txt");
        var s = f.ReadToEnd();
        var img = new Bitmap(@"Images\clipboard.png");
        var s1 = img.ToBase64();
        Image img1 = s1.ToImage()!;
        var bm = new BitmapImage(new Uri(Path.Combine(_baseDirectory, @"Images\clipboard.png"), UriKind.Absolute));
        var s2 = bm.ToBase64();
        var bm1 = img1!.ToBitmapImage();
        var s3 = bm1.ToBase64();
    }

    [Fact]
    public void TestBase64ToImage()
    {
        using var f = File.OpenText(@"Images\clipboard_b64.txt");
        var s = f.ReadToEnd();
        var img1 = s.ToImage();
        var imgBitmap = s.ToBitmapImage();
        var bm = new BitmapImage(new Uri(Path.Combine(_baseDirectory, @"Images\clipboard.png"), UriKind.Absolute));
    }
}
