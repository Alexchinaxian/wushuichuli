using System;
using System.Windows;

namespace IndustrialControlHMI.Converters;

internal static class VisibilityConverterHelper
{
    public static Visibility FromObject(object? value, bool inverse = false)
    {
        var visible = value != null;
        if (inverse) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility FromString(string? value, bool inverse = false)
    {
        var visible = !string.IsNullOrEmpty(value);
        if (inverse) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility FromInt64(long value, bool inverse = false)
    {
        var visible = value > 0;
        if (inverse) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public static bool IsInverseParameter(object? parameter)
        => string.Equals(parameter?.ToString(), "inverse", StringComparison.OrdinalIgnoreCase)
           || string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
}

