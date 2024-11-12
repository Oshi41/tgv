using System.Collections.Specialized;
using System.Net;

namespace sharp_express.core;

public interface IContext
{
    CookieCollection Cookies { get; }
    NameValueCollection Query { get; }
    NameValueCollection ClientHeaders { get; }
    NameValueCollection ResponseHeaders { get; }
    string HttpMethod { get; }
    byte[]? Result { get; }
    string ContentType { get; set; }
    IDictionary<string, string> Parameters { get; set; }
    Uri Url { get; }
    Task<string> Body();
    Task<T> Body<T>();
    Exception Throw(HttpStatusCode code, Exception? e = null);
    void Redirect(string path);
    void Send(byte[] bytes);
    void Send(HttpStatusCode code = HttpStatusCode.BadRequest);
    void Html(string text);
    void Json(object obj);
}