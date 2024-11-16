using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tgv;
using tgv.core;

namespace charsp_express_serve_static;

public static class StaticFilesMiddleware
{
    public static App ServeStatic(this App app, StaticFilesConfig cfg)
    {
        async Task Middleware(Context ctx, Action next, Exception? e)
        {
            if (ctx.Method != HttpMethod.Get && ctx.Method != HttpMethod.Head)
            {
                if (cfg.FallThrough)
                {
                    next();
                    return;
                }
                
                ctx.ResponseHeaders["Content-Length"] = "0";
                ctx.ResponseHeaders["Allow"] = "GET, HEAD";
                ctx.Send(HttpStatusCode.MethodNotAllowed);
                return;
            }

            if (!Directory.Exists(cfg.SourceDirectory))
            {
                ctx.Logger.Fatal($"Source directory {cfg.SourceDirectory} does not exist");
                throw ctx.Throw(HttpStatusCode.InternalServerError);
            }

            var file = ctx.Url.AbsolutePath
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString())
                .Trim(Path.DirectorySeparatorChar, ' ');
            
            if (!file.Contains("."))
                file += $"{Path.DirectorySeparatorChar}index.html";
            
            file = Path.GetFullPath(Path.Combine(cfg.SourceDirectory, file));
            if (!File.Exists(file))
            {
                next();
                return;
            }
            
            if (!file.StartsWith(Path.GetFullPath(cfg.SourceDirectory)))
            {
                ctx.Logger.Debug("Attempt to get file outside of the source directory: {0}", file);
                throw ctx.Throw(HttpStatusCode.BadRequest);
            }

            await ctx.SendFile(file);
        }

        app.After(Middleware);
        return app;
    }
}