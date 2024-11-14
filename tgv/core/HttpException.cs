using System.Net;

namespace tgv.core;

public class HttpException(HttpStatusCode code, string message) : Exception(message)
{
    public HttpStatusCode Code { get; } = code;

    public HttpException(HttpStatusCode code = HttpStatusCode.InternalServerError)
        : this(code, code.ToString())
    {
    }
}