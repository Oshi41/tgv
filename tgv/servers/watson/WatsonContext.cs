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

public class WatsonContext : Context
{
    internal readonly HttpContextBase Ctx;
    private string _body;

    #region Properties

    public override string ContentType { get; set; }
    public override bool WasSent => Ctx.Response.ResponseSent;

    #endregion

    #region Public methods

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    public override async Task<string> Body()
    {
        return Ctx.Request.DataAsString;
    }

    /// <summary>
    /// Redirecting to location
    /// </summary>
    /// <param name="path">location</param>
    /// <param name="code">Redirection code</param>
    public override async Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        Ctx.Response.Headers["Location"] = path;
        Ctx.Response.StatusCode = (int)code;
        
        await BeforeSending();
        await Ctx.Response.Send();
        await AfterSending();
    }

    #endregion

    protected override async Task BeforeSending()
    {
        await base.BeforeSending();

        foreach (string responseHeader in ResponseHeaders)
        {
            Ctx.Response.Headers[responseHeader] = Ctx.Response.Headers[responseHeader];
        }
    }

    protected override async Task SendRaw(byte[] bytes, int code, string contentType)
    {
        ContentType = contentType;
        Ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await Ctx.Response.Send(bytes);
        await AfterSending();
    }

    protected override async Task SendRaw(Stream stream, int code, string contentType)
    {
        ContentType = contentType;
        Ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await Ctx.Response.Send(stream.Length, stream);
        await AfterSending();
    }

    internal WatsonContext(HttpContextBase ctx, Logger logger)
        : base(ctx.Request.Method.Convert(),
            ctx.Request.Method.Convert(),
            ctx.Guid,
            new Uri($"http://{ctx.Request.Source.IpAddress}:{ctx.Request.Source.Port}{ctx.Request.Url.RawWithQuery}"),
            logger,
            ctx.Request.Headers,
            ctx.Request.Url.Parameters)
    {
        Logger.Debug("Start handling request");
    }
}