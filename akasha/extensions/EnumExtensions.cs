using System;
using System.Linq;

namespace akasha.extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Checking if enum has any of provided flags
    /// </summary>
    /// <param name="value">Source value</param>
    /// <param name="values">Possible enum flags</param>
    /// <typeparam name="T">Enum type</typeparam>
    public static bool HasAny<T>(this T value, params T[] values) where T : Enum => values.Any(x => value.HasFlag(x));

    /// <summary>
    /// Resolving enum with flag value
    /// </summary>
    /// <param name="value">Enum source</param>
    /// <param name="flag"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T With<T>(this T value, T flag)
        where T : Enum
    {
        if (value.HasFlag(flag)) return value;

        var res = Convert.ToUInt64(value) | Convert.ToUInt64(flag);
        return (T)Enum.ToObject(value.GetType(), res);
    }
}