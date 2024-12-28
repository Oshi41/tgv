using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace akasha.extensions;

public static class CollectionExtensions
{
    public static int FindIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        var index = 0;

        foreach (var item in collection)
        {
            if (predicate(item))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    public static void Clear<T1, T2>(this ConcurrentDictionary<T1, T2> col, Action<T1, T2> action, int tries = 10)
    {
        bool TryRemove(T1 key, out T2 value)
        {
            for (int i = 0; i < tries; i++)
            {
                if (col.TryRemove(key, out value))
                    return value != null;
            }

            Console.WriteLine("Failed to remove {0} item after {1} tries", key, tries);
            value = default!;
            return false;
        }

        while (col.Count > 0)
        {
            var key = col.Keys.First();
            if (TryRemove(key, out var value))
                action(key, value);
        }
    }
}