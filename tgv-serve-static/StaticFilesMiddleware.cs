using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tgv_core.api;
using tgv_core.imp;
using tgv;

namespace tgv_serve_static;

/// <summary>
/// Simple wrapper for wathing actual files content
/// </summary>
class FileWatcher
{
    // current settings
    private readonly StaticFilesConfig _cfg;
    // app logger
    private readonly Logger _logger;
    // file content cache
    private readonly ConcurrentDictionary<string,byte[]> _cache;
    // working watcher
    private readonly FileSystemWatcher _watcher;

    public FileWatcher(StaticFilesConfig cfg, Logger logger)
    {
        _cfg = cfg;
        _logger = logger;
        _watcher = new FileSystemWatcher(cfg.SourceDirectory);
        _cache = new ConcurrentDictionary<string, byte[]>();
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
        _watcher.EnableRaisingEvents = true;
        
        _watcher.Changed += WatcherOnChanged;
        _watcher.Deleted += WatcherOnDeleted;
        _watcher.Renamed += WatcherOnRenamed;
    }

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        if (_cache.TryRemove(e.OldFullPath, out var content))
        {
            _cache[e.FullPath] = content;
            _logger.Debug($"File renamed {e.OldFullPath} => {e.FullPath}");
        }
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        if (_cache.TryRemove(e.FullPath, out _))
            _logger.Debug($"File {e.FullPath} was deleted");
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        if (_cache.ContainsKey(e.FullPath))
        {
            lock (_watcher)
            {
                _logger.Debug($"File {e.FullPath} was changed, reloading");
                _cache[e.FullPath] = File.ReadAllBytes(e.FullPath);
            }
        }
    }

    public byte[]? this[string filename]
    {
        get
        {
            // filw contains in cache
            if (_cache.TryGetValue(filename, out var content))
                return content;

            // file not exists
            if (!File.Exists(filename)) return null;

            // file is outside the source directory
            if (!filename.StartsWith(Path.GetFullPath(_cfg.SourceDirectory))) return null;

            // reading file and store in cache
            return _cache[filename] = File.ReadAllBytes(filename);
        }
    }
}

public static class StaticFilesMiddleware
{
    public static App ServeStatic(this App app, StaticFilesConfig cfg)
    {
        if (!Directory.Exists(cfg.SourceDirectory))
            throw new Exception("Source directory does not exist");

        var cache = new FileWatcher(cfg, app.Logger);
        
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
                ctx.Logger.Fatal($"Source directory {cfg.SourceDirectory} does not exist");
                throw ctx.Throw(HttpStatusCode.InternalServerError);
            }

            var file = ctx.Url.AbsolutePath
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString())
                .Trim(Path.DirectorySeparatorChar, ' ');
            
            if (!file.Contains("."))
                file += $"index.html";
            
            file = Path.GetFullPath(Path.Combine(cfg.SourceDirectory, file));
            var content = cache[file];
            if (content == null)
            {
                next();
                return;
            }

            await ctx.SendFile(file, content);
        }

        app.After(Middleware);
        return app;
    }
}