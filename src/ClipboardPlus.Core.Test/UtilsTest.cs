using System.Text;
using System.Text.Unicode;
using Xunit.Abstractions;

namespace ClipboardPlus.Core.Test;

public class UtilsTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UtilsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("QQ.exe", 1)]
    [InlineData("", 0)]
    [InlineData("在池台的正中，像当初的怀中，隔太多春秋会不能相拥", 24)]
    [InlineData(
        "Out of the tens of thousands of people, we are fortunate enough to "
            + "meet each other, and in an instant, there is a profound clarity and understanding.",
        27
    )]
    [InlineData("你好，~", 4)]
    [InlineData("你好，Hello啊.\nabc‘ def(), qwe'", 9)]
    public void TestWordsCount(string s, int n)
    {
        var nw = Utils.CountWordsEn(s) + Utils.CountWordsCn(s);
        _testOutputHelper.WriteLine($"En：{Utils.CountWordsEn(s)}, Cn: {Utils.CountWordsCn(s)}");
        _testOutputHelper.WriteLine($"{n}:{nw}");
        Assert.True(n == nw);
    }
}
