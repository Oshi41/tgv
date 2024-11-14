using System.Collections.Specialized;
using System.Net;

namespace tgv.core;

public abstract class IContext
{
    private Dictionary<Type, object> _states = new();
    
    /// <summary>
    /// Current routers hierarchy
    /// </summary>
    public Stack<IMatch> CurrentPath { get; } = new();

    /// <summary>
    /// Current request cookies 
    /// </summary>
    public abstract CookieCollection Cookies { get; }

    /// <summary>
    /// Query params
    /// </summary>
    public abstract NameValueCollection Query { get; }

    /// <summary>
    /// Sent headers
    /// </summary>
    public abstract NameValueCollection ClientHeaders { get; }

    /// <summary>
    /// Headers to send
    /// </summary>
    public abstract NameValueCollection ResponseHeaders { get; }

    /// <summary>
    /// Current HTTP method
    /// </summary>
    public abstract string HttpMethod { get; }

    /// <summary>
    /// Pending result to send
    /// </summary>
    public byte[]? Result { get; protected set; }

    /// <summary>
    /// Request content type
    /// </summary>
    public abstract string ContentType { get; set; }

    /// <summary>
    /// Current route parameters
    /// </summary>
    public IDictionary<string, string>? Parameters { get; set; }
    
    public abstract Guid TraceId { get; }

    /// <summary>
    /// Current handling stage
    /// </summary>
    public HandleStages Stage { get; set; }

    /// <summary>
    /// Current URL
    /// </summary>
    public abstract Uri Url { get; }

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    public abstract Task<string> Body();

    /// <summary>
    /// Parsing JSON format body
    /// </summary>
    /// <typeparam name="T">Body type</typeparam>
    /// <returns>Parsed body</returns>
    public abstract Task<T> Body<T>();

    /// <summary>
    /// Signalizing about error during HTTP message handling
    /// </summary>
    /// <param name="code">HTTP code</param>
    /// <param name="e">Possible inner exceptioon</param>
    /// <returns>Http exception</returns>
    public abstract Exception Throw(HttpStatusCode code, Exception? e = default);

    public abstract Exception Throw(HttpStatusCode code, string message);

    /// <summary>
    /// Redirect to
    /// </summary>
    /// <param name="path"></param>
    public abstract void Redirect(string path);

    /// <summary>
    /// Sending raw bytes
    /// </summary>
    /// <param name="bytes"></param>
    public virtual void Send(byte[] bytes)
    {
        Result = bytes;
    }

    /// <summary>
    /// Send HTTP code
    /// </summary>
    /// <param name="code"></param>
    public virtual void Send(HttpStatusCode code) => Send(code, code.ToString());

    public abstract void Send(HttpStatusCode code, string message);

    /// <summary>
    /// Send HTML / plain text
    /// </summary>
    /// <param name="text"></param>
    public abstract void Html(string text);
    public abstract void Text(string text);

    /// <summary>
    /// Send object parsed as JSON format
    /// </summary>
    /// <param name="obj"></param>
    public abstract void Json(object obj);
}