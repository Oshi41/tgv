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

public class Context
{
    internal readonly HttpContextBase Ctx;
    private string _body;

    #region Properties

    /// <summary>
    /// Current routers hierarchy
    /// </summary>
    public Stack<IMatch> CurrentPath { get; } = new();

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
    /// Current HTTP method
    /// </summary>
    public HttpMethod Method { get; internal set; }

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
    public void Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        CheckBeforeSending();

        Ctx.Response.Headers["Location"] = path;
        Ctx.Response.StatusCode = (int)code;
        Ctx.Response.Send();
    }

    public async Task SendRaw(byte[] bytes, int code, string contentType)
    {
        CheckBeforeSending();

        var resp = Ctx.Response;

        Cookies.WriteHeaders(resp.Headers);
        resp.StatusCode = code;
        resp.ContentType = contentType;
        resp.ContentLength = bytes.Length;
        await resp.Send(bytes);
    }

    public virtual Task Send(string text, HttpStatusCode code, string contentType)
        => SendRaw(Encoding.UTF8.GetBytes(text), (int)code, contentType);

    public virtual Task Send(HttpStatusCode code) => Send(code.ToString(), code, "text/plain");

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

        var fs = File.OpenRead(filename);
        ContentType = MimeTypeMap.GetMimeType(Path.GetExtension(filename));
        await Ctx.Response.Send(fs.Length, fs);
    }

    #endregion

    private void CheckBeforeSending()
    {
        if (WasSent)
        {
            throw new Exception("Request was sent");
        }
    }

    internal Context(HttpContextBase ctx, Logger logger)
    {
        Query = ctx.Request.Query.Elements;
        Url = new Uri($"http://{ctx.Request.Source.IpAddress}:{ctx.Request.Source.Port}{ctx.Request.Url.RawWithQuery}");
        Cookies.Parse(ctx.Request.RetrieveHeaderValue("Cookie") ?? string.Empty);
        Ctx = ctx;
        Logger = logger.WithCustomMessage((_, message, _, _, _)
            => $"[{TraceId}][{Method?.Method}][{ctx.Request.Url.RawWithQuery}] {message}");
        Method = ctx.Request.Method.Convert();
        
        Logger.Debug("Start handling request");
    }
}