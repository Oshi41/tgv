using System.Diagnostics;
using System.Net;
using MimeTypes;
using sharp_express.core;

namespace sharp_express.middleware;

public class StaticFilesConfig
{
    public bool FallThrough { get; set; } = true;
    public string SourceDirectory { get; set; }
}

public static class StaticFilesMiddleware
{
    public static Express ServeStatic(this Express express, StaticFilesConfig cfg)
    {
        async Task Middleware(IContext ctx, Action next, Exception? e)
        {
            if (ctx.Stage != HandleStages.Handle)
            {
                next();
                return;
            }

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
            
            var file = Path.GetFullPath(Path.Join(cfg.SourceDirectory, ctx.Url.AbsolutePath));
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
            
            var bytes = await File.ReadAllBytesAsync(file);
            ctx.Send(bytes);
            ctx.ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(file));
        }

        express.After(Middleware);
        return express;
    }
}