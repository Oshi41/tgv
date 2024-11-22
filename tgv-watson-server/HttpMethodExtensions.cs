using System.Net.Http;

namespace tgv_watson_server;

public static class HttpMethodExtensions
{
    public static HttpMethod Convert(this WatsonWebserver.Core.HttpMethod method)
    {
        return method switch
        {
            WatsonWebserver.Core.HttpMethod.GET => HttpMethod.Get,
            WatsonWebserver.Core.HttpMethod.HEAD => HttpMethod.Head,
            WatsonWebserver.Core.HttpMethod.PUT => HttpMethod.Put,
            WatsonWebserver.Core.HttpMethod.POST => HttpMethod.Post,
            WatsonWebserver.Core.HttpMethod.DELETE => HttpMethod.Delete,
            WatsonWebserver.Core.HttpMethod.PATCH => tgv_common.extensions.HttpMethodExtensions.Patch,
            WatsonWebserver.Core.HttpMethod.CONNECT => tgv_common.extensions.HttpMethodExtensions.Connect,
            WatsonWebserver.Core.HttpMethod.OPTIONS => HttpMethod.Options,
            WatsonWebserver.Core.HttpMethod.TRACE => HttpMethod.Trace,
            _ => tgv_common.extensions.HttpMethodExtensions.Unknown,
        };
    }
}