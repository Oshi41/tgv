using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using tgv_common.api;
using tgv_common.extensions;
using tgv_common.imp;
using WatsonWebserver.Core;

namespace tgv_watson_server;

public class WatsonContext : Context
{
    protected readonly HttpContextBase _ctx;
    private string _body;

    #region Properties

    public override string ContentType { get; set; }
    public override bool WasSent => _ctx.Response.ResponseSent;

    #endregion

    #region Public methods

    /// <summary>
    /// Reading body from request
    /// </summary>
    /// <returns></returns>
    public override async Task<string> Body()
    {
        return _ctx.Request.DataAsString;
    }

    /// <summary>
    /// Redirecting to location
    /// </summary>
    /// <param name="path">location</param>
    /// <param name="code">Redirection code</param>
    public override async Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
    {
        _ctx.Response.Headers["Location"] = path;
        _ctx.Response.StatusCode = (int)code;
        
        await BeforeSending();
        await _ctx.Response.Send();
        await AfterSending();
    }

    #endregion

    protected override async Task BeforeSending()
    {
        await base.BeforeSending();

        foreach (string responseHeader in ResponseHeaders)
        {
            _ctx.Response.Headers[responseHeader] = ResponseHeaders[responseHeader];
        }
    }

    protected override async Task SendRaw(byte[] bytes, int code, string contentType)
    {
        ContentType = contentType;
        _ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await _ctx.Response.Send(bytes);
        await AfterSending();
    }

    protected override async Task SendRaw(Stream stream, int code, string contentType)
    {
        ContentType = contentType;
        _ctx.Response.StatusCode = code;
        
        await BeforeSending();
        await _ctx.Response.Send(stream.Length, stream);
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
        _ctx = ctx;
    }
}