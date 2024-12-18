using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace tgv_core.extensions;

public static class ConditionalWeakTableExtensions
{
    public static IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table) 
        where TKey : class
        where TValue : class
    {
        return (table as IEnumerable<KeyValuePair<TKey, TValue>>)!;
    }
}