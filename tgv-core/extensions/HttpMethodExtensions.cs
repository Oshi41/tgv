using System.Collections.Generic;
using System.Net.Http;

namespace tgv_core.extensions;

public static class HttpMethodExtensions
{
    public static readonly HttpMethod Before = new("BEFORE");
    public static readonly HttpMethod After = new("AFTER");
    public static readonly HttpMethod Error = new("ERROR");
    public static readonly HttpMethod Patch = new("PATCH");
    public static readonly HttpMethod Connect = new("CONNECT");
    public static readonly HttpMethod Unknown = new("UNKNOWN");

    public static bool IsStandardMethod(this HttpMethod method)
    {
        return method == HttpMethod.Get
               || method == HttpMethod.Head
               || method == HttpMethod.Options
               || method == HttpMethod.Post
               || method == HttpMethod.Put
               || method == HttpMethod.Delete
               || method == HttpMethod.Trace
               || method == Patch
               || method == Connect;
    }

    public static IEnumerable<string> GetStandardMethods()
    {
        yield return HttpMethod.Get.Method;
        yield return HttpMethod.Delete.Method;
        yield return HttpMethod.Post.Method;
        yield return HttpMethod.Put.Method;
        yield return HttpMethod.Head.Method;
        yield return HttpMethod.Options.Method;
        yield return HttpMethod.Trace.Method;
        yield return Patch.Method;
        yield return Connect.Method;
    }
}