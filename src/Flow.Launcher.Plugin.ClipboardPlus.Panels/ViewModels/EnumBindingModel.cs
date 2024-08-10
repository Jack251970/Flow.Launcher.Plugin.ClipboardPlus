using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

public class EnumBindingModel<T> where T : struct, Enum
{
    public string Name { get; private set; } = string.Empty;
    public string LocalizationString { get; private set; } = string.Empty;
    public T Value { get; private set; } = default;

    public static IReadOnlyList<EnumBindingModel<T>> CreateList(PluginInitContext? context)
    {
        return Enum.GetValues<T>()
            .Select(value => new EnumBindingModel<T>
            {
                Name = value.ToString(),
                LocalizationString = context.GetTranslation(value),
                Value = value
            })
            .ToArray();
    }

    public EnumBindingModel<T> From(T value, PluginInitContext? context)
    {
        return new EnumBindingModel<T>
        {
            Name = value.ToString(),
            LocalizationString = context.GetTranslation(value),
            Value = value
        };
    }

    public static bool operator ==(EnumBindingModel<T> a, EnumBindingModel<T> b) => a.Equals(b);

    public static bool operator !=(EnumBindingModel<T> a, EnumBindingModel<T> b) => !a.Equals(b);

    public override bool Equals(object? obj)
    {
        if (obj is EnumBindingModel<T> model)
        {
            return Equals(model);
        }
        return false;
    }

    public bool Equals(EnumBindingModel<T> b)
    {
        return Name == b.Name
            && LocalizationString == b.LocalizationString
            && Value.Equals(b.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, LocalizationString, Value);
    }

    public override string ToString()
    {
        return LocalizationString;
    }
}
