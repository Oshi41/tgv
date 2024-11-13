using System.Collections.Specialized;
using System.Net;

namespace sharp_express.core;

public interface IContext
{
    /// <summary>
    /// Current routers hierarchy
    /// </summary>
    Stack<IMatch> CurrentPath { get; }

    /// <summary>
    /// Current request cookies 
    /// </summary>
    CookieCollection Cookies { get; }

    /// <summary>
    /// Query params
    /// </summary>
    NameValueCollection Query { get; }

    /// <summary>
    /// Sent headers
    /// </summary>
    NameValueCollection ClientHeaders { get; }

    /// <summary>
    /// Headers to send
    /// </summary>
    NameValueCollection ResponseHeaders { get; }

    /// <summary>
    /// Current HTTP method
    /// </summary>
    string HttpMethod { get; }

    /// <summary>
    /// Pending result to send
    /// </summary>
    byte[]? Result { get; }

    /// <summary>
    /// Request content type
    /// </summary>
    string ContentType { get; set; }

    /// <summary>
    /// Current route parameters
    /// </summary>
    IDictionary<string, string>? Parameters { get; set; }

    /// <summary>
    /// Current handling stage
    /// </summary>
    HandleStages Stage { get; set; }

    /// <summary>
    /// Current URL
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    Task<string> Body();

    /// <summary>
    /// Parsing JSON format body
    /// </summary>
    /// <typeparam name="T">Body type</typeparam>
    /// <returns>Parsed body</returns>
    Task<T> Body<T>();

    /// <summary>
    /// Signalizing about error during HTTP message handling
    /// </summary>
    /// <param name="code">HTTP code</param>
    /// <param name="e">Possible inner exceptioon</param>
    /// <returns>Http exception</returns>
    Exception Throw(HttpStatusCode code, Exception? e = null);

    Exception Throw(HttpStatusCode code, string message);

    /// <summary>
    /// Redirect to
    /// </summary>
    /// <param name="path"></param>
    void Redirect(string path);

    /// <summary>
    /// Sending raw bytes
    /// </summary>
    /// <param name="bytes"></param>
    void Send(byte[] bytes);

    /// <summary>
    /// Send HTTP code
    /// </summary>
    /// <param name="code"></param>
    void Send(HttpStatusCode code = HttpStatusCode.BadRequest);

    void Send(HttpStatusCode code, string message);

    /// <summary>
    /// Send HTML / plain text
    /// </summary>
    /// <param name="text"></param>
    void Html(string text);

    /// <summary>
    /// Send object parsed as JSON format
    /// </summary>
    /// <param name="obj"></param>
    void Json(object obj);
}