using System;
using System.Net;

namespace tgv_common.imp;

public class HttpException(HttpStatusCode code, string message) : Exception(message)
{
    public HttpStatusCode Code { get; } = code;

    public HttpException(HttpStatusCode code = HttpStatusCode.InternalServerError)
        : this(code, code.ToString())
    {
    }
}