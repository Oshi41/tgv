using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MimeTypes;
using Newtonsoft.Json;
using tgv_core.extensions;
using tgv_core.imp;

[assembly: InternalsVisibleTo("tgv")]
[assembly: InternalsVisibleTo("tgv-tests")]
[assembly: InternalsVisibleTo("tgv-server-kestrel")]

namespace tgv_core.api;

public abstract class Context : IDisposable
{
    #region Public props

    /// <summary>
    /// Current routers hierarchy
    /// </summary>
    public Stack<IMatch> CurrentPath { get; } = new();

    /// <summary>
    /// Contains history of visited routes
    /// </summary>
    public Queue<IMatch> Visited { get; } = new();

    /// <summary>
    /// Current context HTTP method stage
    /// </summary>
    public virtual HttpMethod Stage { get; internal set; } = HttpMethodExtensions.Before;

    /// <summary>
    /// Original response HTTP method. Will not change in any ctx stage
    /// </summary>
    public virtual HttpMethod Method { get; }

    /// <summary>
    /// Current route parameters
    /// </summary>
    public IDictionary<string, string>? Parameters { get; set; }

    public virtual Guid TraceId { get; }

    /// <summary>
    /// Current URL
    /// </summary>
    public virtual Uri Url { get; }

    public Logger Logger { get; }

    public CookieCollection Cookies { get; } = new();

    /// <summary>
    /// Headers to send
    /// </summary>
    public NameValueCollection ResponseHeaders { get; } = new();

    /// <summary>
    /// Sent headers
    /// </summary>
    public NameValueCollection ClientHeaders { get; }

    /// <summary>
    /// Query params
    /// </summary>
    public NameValueCollection Query { get; }

    #endregion

    #region Public Abstract props

    public abstract bool WasSent { get; }

    #endregion

    #region Public methods

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
    {
        throw Throw(code, e?.Message ?? code.ToString());
    }

    public Exception Throw(HttpStatusCode code, string message)
    {
        var e = new HttpException(code, message);
        throw e;
    }

    public Task SendCode(HttpStatusCode code)
        => SendRaw((byte[])null, code, null);

    public virtual Task Send(HttpStatusCode code, string? message = null)
        => Send(message ?? code.ToString(), code, "text/plain");

    public virtual Task Json(object resp)
    {
        var json = JsonConvert.SerializeObject(resp);
        return Send(json, HttpStatusCode.OK, "application/json");
    }

    public virtual Task Html(string html) => Send(html, HttpStatusCode.OK, "text/html");
    public virtual Task Text(string txt) => Send(txt, HttpStatusCode.OK, "text/plain");

    public virtual async Task SendFile(string filename)
    {
        if (!File.Exists(filename)) throw new FileNotFoundException(filename);
        await SendRaw(
            File.OpenRead(filename),
            HttpStatusCode.OK,
            MimeTypeMap.GetMimeType(Path.GetExtension(filename))
        );
    }

    #endregion

    #region Public Abstract methods

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    public abstract Task<string> Body();

    /// <summary>
    /// Redirecting to location
    /// </summary>
    /// <param name="path">location</param>
    /// <param name="code">Redirection code</param>
    public abstract Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved);

    public virtual Task Send(string text, HttpStatusCode code, string contentType)
        => SendRaw(string.IsNullOrEmpty(text)? null : Encoding.UTF8.GetBytes(text), code, contentType);

    #endregion

    #region Protected methods

    protected virtual Task BeforeSending()
    {
        if (WasSent)
        {
            throw new Exception("Request was sent");
        }

        Cookies.Diff(_original).WriteHeaders(ResponseHeaders);
        return Task.CompletedTask;
    }

    protected virtual Task AfterSending()
    {
        RequestFinished?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    #endregion

    #region Protected Abstract methods

    protected abstract Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType);
    protected abstract Task SendRaw(Stream stream, HttpStatusCode code, string contentType);

    #endregion

    public event EventHandler RequestFinished;
    private readonly CookieCollection _original = new();

    /// <summary>
    /// Base context constructor
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="traceId">Uniq request ID</param>
    /// <param name="url">Current URL</param>
    /// <param name="logger">Logger</param>
    /// <param name="headers">Request headers. May be null if no headers provided.</param>
    /// <param name="query">Query parameters. Pass null to automatically parse from URL</param>
    /// <param name="cookies">Request cookies. Pass null to parse from header</param>
    protected Context(HttpMethod method, Guid traceId, Uri url, Logger logger,
        NameValueCollection? headers = null, NameValueCollection? query = null, CookieCollection? cookies = null)
    {
        Method = method;
        TraceId = traceId;
        Url = url;
        Logger = logger;
        ClientHeaders = headers ?? new NameValueCollection();

        Query = query ?? System.Web.HttpUtility.ParseQueryString(url.Query);

        if (cookies != null)
        {
            Cookies.Add(cookies);
            _original.Add(cookies);
        }
        else
        {
            Cookies.Parse(ClientHeaders[HttpRequestHeader.Cookie.ToString()] ?? string.Empty);
            _original.Add(Cookies);
        }
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