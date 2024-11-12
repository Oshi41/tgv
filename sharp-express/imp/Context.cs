using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;

namespace sharp_express.core;

public partial class Context : IContext
{
    private readonly HttpListenerContext _ctx;
    private string _body;
    
    public Context(HttpListenerContext ctx)
    {
        _ctx = ctx;
    }

    public CookieCollection Cookies => _ctx.Response.Cookies;
    public NameValueCollection Query => _ctx.Request.QueryString;
    public NameValueCollection ClientHeaders => _ctx.Request.Headers;
    public NameValueCollection ResponseHeaders => _ctx.Response.Headers;
    public string HttpMethod => _ctx.Request.HttpMethod;

    public byte[]? Result { get; protected set; }
    public string ContentType
    {
        get => _ctx.Response.ContentType;
        set => _ctx.Response.ContentType = value;
    }

    public IDictionary<string, string> Parameters { get; set; }
    public Uri Url => _ctx.Request.Url!;

    public async Task<string> Body()
    {
        if (_body == null)
        {
            using var stream = new MemoryStream();
            await _ctx.Request.InputStream.CopyToAsync(stream);
            var bytes = stream.ToArray();
            _body = Encoding.UTF8.GetString(bytes);
        }

        return _body;
    }

    public async Task<T> Body<T>()
    {
        var body = await Body();
        try
        {
            return JsonConvert.DeserializeObject<T>(body);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Error during body parsing: {0}", e.Message);
            throw Throw(HttpStatusCode.BadRequest);
        }
    }

    public Exception Throw(HttpStatusCode code, Exception? e = null)
    {
        var err = new HttpRequestException(code.ToString(), e, code);
        throw err;
    }

    public void Redirect(string path)
    {
        _ctx.Response.Redirect(path);
    }

    public void Send(byte[] bytes)
    {
        Result = bytes;
    }

    public void Send(HttpStatusCode code = HttpStatusCode.BadRequest)
    {
        Send(Encoding.UTF8.GetBytes(code.ToString()));
        _ctx.Response.StatusCode = (int)code;
    }

    public void Html(string text)
    {
        Send(Encoding.UTF8.GetBytes(text));
        ContentType = "text/html";
    }
    
    public void Json(object obj)
    {
        Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        ContentType = "application/json";
    }
}