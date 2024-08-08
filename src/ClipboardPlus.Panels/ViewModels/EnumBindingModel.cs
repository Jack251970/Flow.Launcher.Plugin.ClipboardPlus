using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ClipboardPlus.Panels.ViewModels;

public class EnumBindingModel<T> where T : struct, Enum
{
    internal PluginInitContext? Context { get; set; }

    public EnumBindingModel(PluginInitContext? context)
    {
        Context = context;
    }

    public static IReadOnlyList<EnumBindingModel<T>> CreateList(PluginInitContext? context)
    {
        return Enum.GetValues<T>()
            .Select(value => new EnumBindingModel<T>(context)
            {
                Value = value, LocalizationKey = GetDescriptionAttr(value)
            })
            .ToArray();
    }

    public EnumBindingModel<T> From(T value, PluginInitContext context)
    {
        var name = value.ToString();
        var description = GetDescriptionAttr(value);
        
        return new EnumBindingModel<T>(context)
        {
            Name = name,
            LocalizationKey = description,
            Value = value
        };
    }

    private static string GetDescriptionAttr(T source)
    {
        var fi = source.GetType().GetField(source.ToString());

        var attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false)!;

        return attributes is { Length: > 0 } ? attributes[0].Description : source.ToString();
    }
    
    public string Name { get; set; } = string.Empty;
    private string LocalizationKey { get; set; } = string.Empty;
    public string Description => Context?.API.GetTranslation(LocalizationKey) ?? LocalizationKey;
    public T Value { get; set; }
}
