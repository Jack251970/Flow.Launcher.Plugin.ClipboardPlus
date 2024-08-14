namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Extensions;

public static class EnumExtensions
{
    #region Keep Time

    private static readonly Dictionary<KeepTime, int> KeepTimeDict =
        new(
            new List<KeyValuePair<KeepTime, int>>()
            {
                new(KeepTime.Always, int.MaxValue),
                new(KeepTime.Hour1, 1),
                new(KeepTime.Hours12, 12),
                new(KeepTime.Hours24, 24),
                new(KeepTime.Days3, 72),
                new(KeepTime.Days7, 168),
                new(KeepTime.Month1, 720),
                new(KeepTime.Months6, 4320),
                new(KeepTime.Year1, 8640),
            }
        );

    public static int ToKeepTime(this KeepTime idx)
    {
        var k = KeepTimeDict.ContainsKey(idx) ? idx : 0;
        return KeepTimeDict[k];
    }

    #endregion
}
