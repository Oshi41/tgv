namespace tgv.extensions;

public static class HttpMethodExtensions
{
    public static HttpMethod Before = new("BEFORE");
    public static HttpMethod After = new("AFTER");
    public static HttpMethod Error = new("ERROR");
    public static HttpMethod Patch = new("PATCH");
    public static HttpMethod Connect = new("CONNECT");
    public static HttpMethod Unknown = new("UNKNOWN");

    internal static HttpMethod Convert(this WatsonWebserver.Core.HttpMethod method)
    {
        return method switch
        {
            WatsonWebserver.Core.HttpMethod.GET => HttpMethod.Get,
            WatsonWebserver.Core.HttpMethod.HEAD => HttpMethod.Head,
            WatsonWebserver.Core.HttpMethod.PUT => HttpMethod.Put,
            WatsonWebserver.Core.HttpMethod.POST => HttpMethod.Post,
            WatsonWebserver.Core.HttpMethod.DELETE => HttpMethod.Delete,
            WatsonWebserver.Core.HttpMethod.PATCH => Patch,
            WatsonWebserver.Core.HttpMethod.CONNECT => Connect,
            WatsonWebserver.Core.HttpMethod.OPTIONS => HttpMethod.Options,
            WatsonWebserver.Core.HttpMethod.TRACE => HttpMethod.Trace,
            _ => Unknown,
        };
    }
}