using System.Runtime.CompilerServices;

namespace sharp_express.middleware.session;

public static class SessionMiddleware
{
    private static readonly ConditionalWeakTable<Express, SessionStore> _states = new();

    public static void UseSession(this Express app, SessionConfig config)
    {
        app.Use(async (context, next, _) =>
        {
            var ctx = await app.GetSessionStore()!.GetContext(context, true);
            ctx!.Touch();
            next();
        });

        app.Started += (_, __) =>
        {
            _states.Add(app, new SessionStore(app, config));
        };
        app.Closed += (_, _) =>
        {
            _states.Remove(app);
        };
    }

    public static SessionStore? GetSessionStore(this Express app)
    {
        return _states.TryGetValue(app, out var store)
            ? store
            : null;
    }
}