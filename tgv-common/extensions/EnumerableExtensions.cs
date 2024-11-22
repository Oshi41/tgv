using System.Collections.Generic;
using System.Linq;

namespace tgv_common.extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T?> SkipLast<T>(this IEnumerable<T?> source, int count)
    {
        // todo implement better
        return source.Reverse().Skip(count).Reverse();
    }
}