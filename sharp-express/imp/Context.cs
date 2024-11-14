using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;
using MimeTypes;
using Newtonsoft.Json;
using sharp_express.core;

namespace sharp_express.imp;

public partial class Context : IContext
{
    private readonly HttpListenerContext _ctx;
    private string _body;

    public Context(HttpListenerContext ctx)
    {
        _ctx = ctx;
    }

    public override CookieCollection Cookies => _ctx.Response.Cookies;
    public override NameValueCollection Query => _ctx.Request.QueryString;
    public override NameValueCollection ClientHeaders => _ctx.Request.Headers;
    public override NameValueCollection ResponseHeaders => _ctx.Response.Headers;

    public override string HttpMethod => _ctx.Request.HttpMethod;

    public override string ContentType
    {
        get => _ctx.Response.ContentType;
        set => _ctx.Response.ContentType = value;
    }

    public override Guid TraceId => _ctx.Request.RequestTraceIdentifier;
    public override Uri Url => _ctx.Request.Url!;

    public override async Task<string> Body()
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

    public override async Task<T> Body<T>()
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

    public override Exception Throw(HttpStatusCode code, Exception? e = null)
    {
        throw Throw(code, e?.Message ?? code.ToString());
    }

    public override Exception Throw(HttpStatusCode code, string message)
    {
        var err = new HttpException(code, message);
        throw err;
    }

    public override void Redirect(string path)
    {
        _ctx.Response.Redirect(path);
    }

    public override void Send(byte[] bytes)
    {
        Result = bytes;
    }

    public override void Send(HttpStatusCode code, string message)
    {
        Send(Encoding.UTF8.GetBytes(code.ToString()));
        _ctx.Response.StatusCode = (int)code;
    }

    public override void Html(string text)
    {
        Send(Encoding.UTF8.GetBytes(text));
        ContentType = "text/html";
    }

    public override void Text(string text)
    {
        Send(Encoding.UTF8.GetBytes(text));
        ContentType = "text/plain";
    }

    public override void Json(object obj)
    {
        Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        ContentType = "application/json";
    }
}