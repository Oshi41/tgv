using System.Net.Http;

namespace tgv_core.extensions;

public static class HttpMethodExtensions
{
    public static HttpMethod Before = new("BEFORE");
    public static HttpMethod After = new("AFTER");
    public static HttpMethod Error = new("ERROR");
    public static HttpMethod Patch = new("PATCH");
    public static HttpMethod Connect = new("CONNECT");
    public static HttpMethod Unknown = new("UNKNOWN");
}