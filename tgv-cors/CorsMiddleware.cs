using System.Linq;
using System.Net.Http;
using tgv_core.api;

namespace tgv_cors;

public static class CorsMiddleware
{
    public static HttpHandler Cors(CorsSettings? settings = null) => async (ctx, next, _) =>
    {
        settings ??= new CorsSettings(["*"]);
        var origin = ctx.ClientHeaders["Origin"];

        // no origin founded
        if (string.IsNullOrEmpty(origin))
        {
            next();
            return;
        }

        if (await settings.ValidateOrigin(ctx))
            ctx.ResponseHeaders["Access-Control-Allow-Origin"] = origin;
        if (settings.UseCredentials)
            ctx.ResponseHeaders["Access-Control-Allow-Credentials"] = "true";
        if (settings.ExposedHeaders?.Any() == true)
            ctx.ResponseHeaders["Access-Control-Expose-Headers"] = string.Join(",", settings.ExposedHeaders);

        // continue all request except preflight
        if (ctx.Method != HttpMethod.Options)
        {
            next();
            return;
        }

        // preflight
        if (settings.Methods?.Any() == true)
            ctx.ResponseHeaders["Access-Control-Allow-Methods"] =
                string.Join(",", settings.Methods.Select(x => x.Method));
        if (settings.MaxAge != null)
            ctx.ResponseHeaders["Access-Control-Max-Age"] = settings.MaxAge.Value.ToString();
        if (settings.AllowedHeaders?.Any() == true)
            ctx.ResponseHeaders["Access-Control-Allow-Headers"] = string.Join(",", settings.AllowedHeaders);

        // continue routing
        if (settings.ContinuePreflight)
        {
            next();
            return;
        }

        await ctx.SendCode(settings.Code);
    };
}