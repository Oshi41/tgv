using System.Diagnostics;
using System.Net;
using MimeTypes;
using sharp_express.core;

namespace sharp_express.middleware.static_serve;

public static class StaticFilesMiddleware
{
    public static Express ServeStatic(this Express express, StaticFilesConfig cfg)
    {
        async Task Middleware(IContext ctx, Action next, Exception? e)
        {
            if (ctx.HttpMethod is not "GET" and not "HEAD")
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
                await Console.Error.WriteLineAsync($"Source directory {cfg.SourceDirectory} does not exist");
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
                Debug.WriteLine("Attempt to get file outside of the source directory: {0}", file);
                throw ctx.Throw(HttpStatusCode.BadRequest);
            }
            
            var bytes = File.ReadAllBytes(file);
            ctx.Send(bytes);
            ctx.ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(file));
        }

        express.After(Middleware);
        return express;
    }
}