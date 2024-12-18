using System.Runtime.CompilerServices;
using tgv_core.api;

namespace tgv;

public static class Extensions
{
    private static readonly ConditionalWeakTable<Context, App> _ctx = new();

    internal static void Associate(this App app, Context ctx)
    {
        _ctx.Add(ctx, app);
    }

    /// <summary>
    /// Return assotiated app
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static App? GetApp(this Context? ctx)
    {
        if (ctx == null)
            return null;
        
        if (_ctx.TryGetValue(ctx, out App? app) && app is not null) return app;
        ctx.Logger.Error("Context's Application not found");
        
        return null;
    }
}