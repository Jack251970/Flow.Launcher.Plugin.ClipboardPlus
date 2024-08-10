using System.Drawing;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class ExtensionTest
{
    private readonly ITestOutputHelper _testOutputHelper;

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
        var bm = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
        _testOutputHelper.WriteLine(bm.Format.ToString());
    }

    [Fact]
    public void TestImageToBase64()
    {
        using var f = File.OpenText(@"Images\clipboard_b64.txt");
        var s = f.ReadToEnd();
        var img = new Bitmap(@"Images\clipboard.png");
        var s1 = img.ToBase64();
        Image img1 = s1.ToImage();

        var imgBitmap = img1.ToBitmapImage();
        var sBitmap = imgBitmap.ToBase64();
    }

    [Fact]
    public void TestBase64ToImage()
    {
        using var f = File.OpenText(@"Images\clipboard_b64.txt");
        var s = f.ReadToEnd();
        var img1 = s.ToImage();
        var imgBitmap = s.ToBitmapImage();
    }

    [Theory]
    [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
    [InlineData("Test", "0CBC6611F5540BD0809A388DC95A615B")]
    public void TestStringToMd5(string s, string md5)
    {
        _testOutputHelper.WriteLine(s.GetMd5());
        Assert.True(s.GetMd5() == md5);
    }
    
    private static readonly DateTime _testDateTime = new(2021, 1, 1, 12, 34, 56);   

    [Theory]
    [InlineData("yyyy-MM-dd-hhmmss-app", "app", "2021-01-01-123456-app")]
    [InlineData("app-yyyy-MM-dd", "app", "app-2021-01-01")]
    public void TestFormatString(string format, string appname, string result)
    {
        var s = StringUtils.FormatImageName(format, _testDateTime, appname);
        Assert.True(s == result);
    }
}
