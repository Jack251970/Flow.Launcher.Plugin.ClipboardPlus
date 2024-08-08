using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ClipboardPlus.Panels.ViewModels;

public class EnumBindingModel<T> where T : struct, Enum
{
    public string Name { get; private set; } = string.Empty;
    public string LocalizationKey { get; private set; } = string.Empty;
    public string LocalizationString { get; private set; } = string.Empty;
    public T Value { get; private set; } = default;

    public static IReadOnlyList<EnumBindingModel<T>> CreateList(PluginInitContext? context)
    {
        return Enum.GetValues<T>()
            .Select(value => new EnumBindingModel<T>
            {
                Name = value.ToString(),
                LocalizationKey = GetDescriptionAttr(value),
                LocalizationString = context?.API.GetTranslation(GetDescriptionAttr(value)) ?? GetDescriptionAttr(value),
                Value = value
            })
            .ToArray();
    }

    public EnumBindingModel<T> From(T value, PluginInitContext? context)
    {
        var name = value.ToString();
        var description = GetDescriptionAttr(value);
        
        return new EnumBindingModel<T>
        {
            Name = name,
            LocalizationKey = description,
            LocalizationString = context?.API.GetTranslation(description) ?? description,
            Value = value
        };
    }

    private static string GetDescriptionAttr(T source)
    {
        var fi = source.GetType().GetField(source.ToString());

        var attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false)!;

        return attributes is { Length: > 0 } ? attributes[0].Description : source.ToString();
    }

    public static bool operator ==(EnumBindingModel<T> a, EnumBindingModel<T> b) => a.Equals(b);

    public static bool operator !=(EnumBindingModel<T> a, EnumBindingModel<T> b) => !a.Equals(b);

    public override bool Equals(object? obj)
    {
        if (obj is EnumBindingModel<T> model)
        {
            return Name == model.Name
                && LocalizationKey == model.LocalizationKey
                && LocalizationString == model.LocalizationString
                && Value.Equals(model.Value);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, LocalizationKey, LocalizationString, Value);
    }

    public override string ToString()
    {
        return LocalizationString;
    }
}
