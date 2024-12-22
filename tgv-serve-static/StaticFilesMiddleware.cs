using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using tgv_core.api;
using tgv;

namespace tgv_serve_static;

public static class StaticFilesMiddleware
{
    public static App ServeStatic(this App app, StaticFilesConfig cfg)
    {
        if (!Directory.Exists(cfg.SourceDirectory))
            throw new Exception("Source directory does not exist");

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
                await ctx.Send(HttpStatusCode.MethodNotAllowed);
                return;
            }

            if (!Directory.Exists(cfg.SourceDirectory))
            {
                ctx.Logger.Fatal("Source directory {dir} does not exist", cfg.SourceDirectory);
                throw ctx.Throw(HttpStatusCode.InternalServerError);
            }

            var file = ctx.Url.AbsolutePath
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString())
                .Trim(Path.DirectorySeparatorChar, ' ');
            
            if (!file.Contains("."))
                file += $"index.html";
            
            file = Path.GetFullPath(Path.Combine(cfg.SourceDirectory, file));
            if (!File.Exists(file))
            {
                next();
                return;
            }

            await ctx.SendFile(File.OpenRead(file), Path.GetFileName(file));
        }

        app.After(Middleware);
        return app;
    }
}