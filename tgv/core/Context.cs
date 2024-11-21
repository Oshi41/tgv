using System.Collections.Specialized;
using System.Net;
using System.Text;
using MimeTypes;
using Newtonsoft.Json;
using tgv.extensions;
using tgv.imp;
using WatsonWebserver.Core;
using HttpMethod = System.Net.Http.HttpMethod;

namespace tgv.core;

public class Context : IDisposable
{
    internal readonly HttpContextBase Ctx;
    private string _body;

    #region Properties

    /// <summary>
    /// Current routers hierarchy
    /// </summary>
    public Stack<IMatch> CurrentPath { get; } = new();

    /// <summary>
    /// Contains history of visited routes
    /// </summary>
    public Queue<IMatch> Visited { get; } = new();

    /// <summary>
    /// Current request cookies 
    /// </summary>
    public CookieCollection Cookies { get; } = new();

    /// <summary>
    /// Query params
    /// </summary>
    public NameValueCollection Query { get; }

    /// <summary>
    /// Sent headers
    /// </summary>
    public NameValueCollection ClientHeaders => Ctx.Request.Headers;

    /// <summary>
    /// Headers to send
    /// </summary>
    public NameValueCollection ResponseHeaders => Ctx.Response.Headers;

    /// <summary>
    /// Current context HTTP method stage
    /// </summary>
    public HttpMethod Stage { get; internal set; }

    /// <summary>
    /// Original response HTTP method. Will not change in any ctx stage
    /// </summary>
    public HttpMethod ResponseMethod { get; }

    /// <summary>
    /// Request content type
    /// </summary>
    public string ContentType
    {
        get => Ctx.Response.ContentType;
        set => Ctx.Response.ContentType = value;
    }

    /// <summary>
    /// Current route parameters
    /// </summary>
    public IDictionary<string, string>? Parameters { get; set; }

    public Guid TraceId => Ctx.Guid;

    /// <summary>
    /// Current URL
    /// </summary>
    public Uri Url { get; }

    public bool WasSent => Ctx.Response.ResponseSent;

    public Logger Logger { get; }

    public event EventHandler RequestFinished;

    #endregion

    #region Public methods

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    public async Task<string> Body()
    {
        return Ctx.Request.DataAsString;
    }

    /// <summary>
    /// Parsing JSON format body
    /// </summary>
    /// <typeparam name="T">Body type</typeparam>
    /// <returns>Parsed body</returns>
    public async Task<T> Body<T>() => JsonConvert.DeserializeObject<T>(await Body());

    /// <summary>
    /// Signalizing about error during HTTP message handling
    /// </summary>
    /// <param name="code">HTTP code</param>
    /// <param name="e">Possible inner exceptioon</param>
    /// <returns>Http exception</returns>
    public Exception Throw(HttpStatusCode code, Exception? e = default)
        => Throw(code, e?.Message ?? code.ToString());

    public Exception Throw(HttpStatusCode code, string message)
    {
        var e = new HttpException(code, message);
        throw e;
    }

    /// <summary>
    /// Redirecting to location
    /// </summary>
    /// <param name="path">location</param>
    /// <param name="code">Redirection code</param>
    public async Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        var afterResponse = BeforeSending();

        Ctx.Response.Headers["Location"] = path;
        Ctx.Response.StatusCode = (int)code;
        await Ctx.Response.Send();
        afterResponse();
    }

    public virtual Task Send(string text, HttpStatusCode code, string contentType)
        => SendRaw(Encoding.UTF8.GetBytes(text), (int)code, contentType);

    public virtual Task Send(HttpStatusCode code, string? message = null)
        => Send(message ?? code.ToString(), code, "text/plain");

    public virtual Task Json(object resp)
    {
        ContentType = "application/json";
        var json = JsonConvert.SerializeObject(resp);
        return Send(json, HttpStatusCode.OK, "application/json");
    }

    public virtual Task Html(string html) => Send(html, HttpStatusCode.OK, "text/html");

    public virtual Task Text(string txt) => Send(txt, HttpStatusCode.OK, "text/plain");

    public virtual async Task SendFile(string filename)
    {
        if (!File.Exists(filename)) throw new FileNotFoundException(filename);

        var afterResponse = BeforeSending();

        var fs = File.OpenRead(filename);
        ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(filename));
        await Ctx.Response.Send(fs.Length, fs);
        afterResponse();
    }

    #endregion

    private Action BeforeSending()
    {
        if (WasSent)
        {
            throw new Exception("Request was sent");
        }

        var existing = new CookieCollection();
        existing.Parse(Ctx.Request.RetrieveHeaderValue("Cookie") ?? string.Empty);

        var save = Cookies.OfType<Cookie>().Except(existing.OfType<Cookie>()).ToList();
        if (save.Any())
        {
            existing = new CookieCollection();
            foreach (var cookie in save)
                existing.Add(cookie);

            // save cookies
            existing.WriteHeaders(Ctx.Response.Headers);
        }


        return () => RequestFinished?.Invoke(this, EventArgs.Empty);
    }

    private async Task SendRaw(byte[] bytes, int code, string contentType)
    {
        var afterSending = BeforeSending();

        var resp = Ctx.Response;
        resp.StatusCode = code;
        resp.ContentType = contentType;
        resp.ContentLength = bytes.Length;
        await resp.Send(bytes);
        afterSending();
    }

    internal Context(HttpContextBase ctx, Logger logger)
    {
        Query = ctx.Request.Query.Elements;
        Url = new Uri($"http://{ctx.Request.Source.IpAddress}:{ctx.Request.Source.Port}{ctx.Request.Url.RawWithQuery}");
        Cookies.Parse(ctx.Request.RetrieveHeaderValue("Cookie") ?? string.Empty);
        Ctx = ctx;
        Logger = logger.WithCustomMessage((_, message, _, _, _)
            => $"[{TraceId}][{Stage?.Method}][{ctx.Request.Url.RawWithQuery}] {message}");
        ResponseMethod = Stage = ctx.Request.Method.Convert();

        Logger.Debug("Start handling request");
    }

    public virtual void Dispose()
    {
        foreach (var callback in RequestFinished?.GetInvocationList()?.OfType<EventHandler>()
                                 ?? Enumerable.Empty<EventHandler>().ToArray())
        {
            RequestFinished -= callback;
        }

        Visited.Clear();
        CurrentPath.Clear();
    }
}