using Xunit;
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
}
