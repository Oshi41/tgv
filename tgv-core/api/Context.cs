using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MimeTypes;
using Newtonsoft.Json;
using NLog;
using tgv_core.extensions;
using tgv_core.imp;

[assembly: InternalsVisibleTo("tgv")]
[assembly: InternalsVisibleTo("tgv-tests")]
[assembly: InternalsVisibleTo("tgv-server-kestrel")]

namespace tgv_core.api;

/// <summary>
/// Represents an abstract context for handling HTTP requests and responses within an application.
/// </summary>
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

    /// <summary>
    /// Unique identifier for tracing requests through the system.
    /// </summary>
    public virtual Guid TraceId { get; }

    /// <summary>
    /// Current URL
    /// </summary>
    public virtual Uri Url { get; }

    /// <summary>
    /// Provides functionality for logging messages with different verbosity levels.
    /// </summary>
    public Logger Logger { get; }

    /// <summary>
    /// Collection of cookies associated with the current context.
    /// </summary>
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

    /// <summary>
    /// Indicates whether the response is sending / has been sent.
    /// </summary>
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

    /// <summary>
    /// Throws an HTTP exception with the specified status code and message.
    /// </summary>
    /// <param name="code">The HTTP status code to associate with the exception.</param>
    /// <param name="message">The message to associate with the exception.</param>
    /// <returns>The thrown exception.</returns>
    /// <exception cref="HttpException">Thrown when the method is called with the specified arguments.</exception>
    public Exception Throw(HttpStatusCode code, string message)
    {
        var e = new HttpException(code, message);
        throw e;
    }

    /// <summary>
    /// Sends an HTTP response with the specified status code.
    /// </summary>
    /// <param name="code">The HTTP status code to send in the response.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public Task SendCode(HttpStatusCode code) => SendRaw((byte[]?)null, code, null);

    /// <summary>
    /// Sends a response with the specified HTTP status code and optional message.
    /// </summary>
    /// <param name="code">The HTTP status code to be sent in the response.</param>
    /// <param name="message">Optional message to include in the response body. Defaults to the string representation of the status code if not provided.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    public virtual Task Send(HttpStatusCode code, string? message = null)
        => Send(message ?? code.ToString(), code, "text/plain");

    /// <summary>
    /// Converts the provided response object to a JSON string and sends it
    /// with an HTTP status code of OK and a content type of "application/json".
    /// </summary>
    /// <param name="resp">The response object to be serialized into JSON format.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public virtual Task Json(object resp)
    {
        var json = JsonConvert.SerializeObject(resp);
        return Send(json, HttpStatusCode.OK, "application/json");
    }

    /// <summary>
    /// Sends the specified HTML content with an HTTP status code of OK (200).
    /// </summary>
    /// <param name="html">The HTML content to send as part of the response.</param>
    /// <returns>A task representing the asynchronous operation of sending the HTML content.</returns>
    public virtual Task Html(string html) => Send(html, HttpStatusCode.OK, "text/html");

    /// <summary>
    /// Sends a text response with the specified content.
    /// </summary>
    /// <param name="txt">The text content to be sent in the response.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task Text(string txt) => Send(txt, HttpStatusCode.OK, "text/plain");

    /// <summary>
    /// Sends a file to the client with the appropriate MIME type based on the file extension.
    /// </summary>
    /// <param name="filename">The name of the file to be sent.</param>
    /// <param name="content">The binary content of the file.</param>
    /// <returns>An asynchronous task representing the operation.</returns>
    public virtual async Task SendFile(string filename, byte[] content)
    {
        await SendRaw(
            content,
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

    /// <summary>
    /// Sends a response with specified text, HTTP status code, and content type.
    /// </summary>
    /// <param name="text">The text content to be sent in the response.</param>
    /// <param name="code">The HTTP status code for the response.</param>
    /// <param name="contentType">The MIME type of the content being sent.</param>
    /// <returns>A task representing the asynchronous operation of sending the response.</returns>
    public virtual Task Send(string text, HttpStatusCode code, string contentType)
        => SendRaw(string.IsNullOrEmpty(text) ? null : Encoding.UTF8.GetBytes(text), code, contentType);

    #endregion

    #region Protected methods

    /// <summary>
    /// Executes operations that should occur before sending a response.
    /// Writing cookies to header list
    /// </summary>
    /// <returns>A completed task if executed successfully.</returns>
    /// <exception cref="Exception">Thrown if a response has already been sent.</exception>
    protected virtual Task BeforeSending()
    {
         Statics.GetMetric().CreateCounter<long>($"{this.GetMetricName()}.before_sending").Add(1);
        
        if (WasSent)
        {
            throw new Exception("Request was sent");
        }

        var written = Cookies.Diff(_original).WriteHeaders(ResponseHeaders);
        
        if (written > 0)
        {
             Statics.GetMetric().CreateHistogram<int>($"{this.GetMetricName()}.cookies_sent")
                .Record(written, this.ToTagsFull());
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the RequestFinished event after the request has been sent and processed.
    /// </summary>
    /// <returns>A completed task representing the operation.</returns>
    protected virtual Task AfterSending()
    {
        RequestFinished?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    #endregion

    #region Protected Abstract methods

    /// <summary>
    /// Sends raw byte data as a response.
    /// </summary>
    /// <param name="bytes">The byte array data to be sent. Can be null to indicate no body content.</param>
    /// <param name="code">The HTTP status code for the response.</param>
    /// <param name="contentType">The content type of the response. Can be null if not applicable.</param>
    /// <returns>A task that represents the asynchronous operation of sending a raw response.</returns>
    public abstract Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType);

    /// <summary>
    /// Send file from stream over the HTTP
    /// </summary>
    /// <param name="stream">Stream need to be sent. Must support "Length" property!</param>
    /// <param name="filename">possible filename, otherwise will use system file name</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public abstract Task SendFile(Stream stream, string filename);

    #endregion

    /// <summary>
    /// Event triggered when the request has finished processing.
    /// </summary>
    public event EventHandler RequestFinished;

    /// <summary>
    /// Stores the original cookies received.
    /// </summary>
    private readonly CookieCollection _original = new();

    /// <summary>
    /// Base context constructor
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="traceId">Uniq request ID</param>
    /// <param name="url">Current URL</param>
    /// <param name="metrics">Metrics object</param>
    /// <param name="headers">Request headers. May be null if no headers provided.</param>
    /// <param name="query">Query parameters. Pass null to automatically parse from URL</param>
    /// <param name="cookies">Request cookies. Pass null to parse from header</param>
    protected Context(HttpMethod method, Guid traceId, Uri url, NameValueCollection? headers = null,
        NameValueCollection? query = null, CookieCollection? cookies = null)
    {
        Method = method;
        TraceId = traceId;
        Url = url;
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

        Logger = LogManager.LogFactory.GetLogger("HTTP request")
            .WithProperty("_01method", method.ToString().ToUpper())
            .WithProperty("_02url", Url.PathAndQuery);
        
        Logger.Debug("TraceID={0}", traceId);
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