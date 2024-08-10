using System.ComponentModel;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Extensions;

public static class TranslationExtensions
{
    public static string GetTranslation(this PluginInitContext? context, string key)
    {
        return context?.API.GetTranslation(key) ?? key;
    }

    public static string GetTranslation(this PluginInitContext? context, Enum value)
    {
        var description = GetDescriptionAttr(value);
        return context.GetTranslation(description);
    }

    private static string GetDescriptionAttr(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[])field?.GetCustomAttributes(typeof(DescriptionAttribute), false)!;
        return attributes is { Length: > 0 } ? attributes[0].Description : value.ToString();
    }
}
