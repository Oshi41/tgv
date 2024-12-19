using System.Collections.Generic;
using System.Linq;
using tgv_core.api;

namespace tgv_core.extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T?> SkipLast<T>(this IEnumerable<T?> source, int count)
    {
        // todo implement better
        return source.Reverse().Skip(count).Reverse();
    }

    public static IEnumerable<IMatch> AllRoutes(this IRouter app)
    {
        var list = new List<IMatch> { app };
        list.AddRange(app.AsEnumerable());
        foreach (var match in app.OfType<IRouter>())
        {
            list.AddRange(match.AllRoutes());
        }

        return list;
    }
}