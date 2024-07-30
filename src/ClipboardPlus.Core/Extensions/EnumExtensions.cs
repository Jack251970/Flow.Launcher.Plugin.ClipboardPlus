namespace ClipboardPlus.Core.Extensions;

public static class EnumExtensions
{
    // Keep time
    private static readonly Dictionary<RecordKeepTime, int> KeepTimeDict =
        new(
            new List<KeyValuePair<RecordKeepTime, int>>()
            {
                new(RecordKeepTime.Always, int.MaxValue),
                new(RecordKeepTime.Hour1, 1),
                new(RecordKeepTime.Hours12, 12),
                new(RecordKeepTime.Hours24, 24),
                new(RecordKeepTime.Days3, 72),
                new(RecordKeepTime.Days7, 168),
                new(RecordKeepTime.Month1, 720),
                new(RecordKeepTime.Months6, 4320),
                new(RecordKeepTime.Year1, 8640),
            }
        );

    public static int ToKeepTime(this RecordKeepTime idx)
    {
        var k = KeepTimeDict.ContainsKey(idx) ? idx : 0;
        return KeepTimeDict[k];
    }
}
