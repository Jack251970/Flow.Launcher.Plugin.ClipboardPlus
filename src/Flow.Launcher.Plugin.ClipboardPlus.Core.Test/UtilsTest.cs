using Xunit;
using Xunit.Abstractions;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class UtilsTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UtilsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("QQ.exe", 1, 0)]
    [InlineData("", 0, 0)]
    [InlineData("在池台的正中，像当初的怀中，隔太多春秋会不能相拥", 0, 24)]
    [InlineData(
        "Out of the tens of thousands of people, we are fortunate enough to "
            + "meet each other, and in an instant, there is a profound clarity and understanding.",
        27, 0
    )]
    [InlineData("你好，~", 1, 3)]
    public void TestWordsCount(string s, int en, int cn)
    {
        var en1 = StringUtils.CountWordsEn(s);
        var cn1 = StringUtils.CountWordsCn(s);
        _testOutputHelper.WriteLine($"En：{en1}");
        _testOutputHelper.WriteLine($"Cn: {cn1}");
        Assert.True(en == en1 && cn == cn1);
    }

    [Theory]
    [InlineData("hello", 4, "h...")]
    [InlineData("hello", 5, "hello")]
    [InlineData("hello", 6, "hello")]
    [InlineData("你好！", 5, "你...")]
    [InlineData("你好📌", 5, "你...")]
    [InlineData("你a好！", 6, "你a...")]
    [InlineData("你好！", 6, "你好！")]
    public void TestCompressString(string s, int en_length, string expected)
    {
        var result = StringUtils.CompressString(s, en_length);
        _testOutputHelper.WriteLine($"Result: {result}");
        Assert.Equal(result, expected);
    }

    private static readonly DateTime _testDateTime = new(2021, 1, 1, 12, 34, 56);

    [Theory]
    [InlineData("yyyy-MM-dd-hhmmss-app", "app", "2021-01-01-123456-app")]
    [InlineData("app-yyyy-MM-dd", "app", "app-2021-01-01")]
    public void TestFormatString(string format, string appname, string result)
    {
        var s = StringUtils.FormatImageName(format, _testDateTime, appname);
        Assert.Equal(s, result);
    }

    [Theory]
    [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
    [InlineData("Test", "0CBC6611F5540BD0809A388DC95A615B")]
    public void TestStringToMd5(string s, string md5)
    {
        _testOutputHelper.WriteLine(StringUtils.GetMd5(s));
        Assert.Equal(StringUtils.GetMd5(s), md5);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("你好")]
    [InlineData("📌")]
    public void TestEncrypt(string s)
    {
        var key = StringUtils.GenerateAESKey();
        _testOutputHelper.WriteLine(key);
        var e = StringUtils.Encrypt(s, key);
        _testOutputHelper.WriteLine(e);
        var d = StringUtils.Decrypt(e, key);
        _testOutputHelper.WriteLine(d);
        Assert.Equal(s, d);
    }
}
