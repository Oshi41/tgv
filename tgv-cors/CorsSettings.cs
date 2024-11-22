using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tgv_common.api;
using tgv_common.extensions;

namespace tgv_cors;

public class CorsSettings
{
    /// <summary>
    /// Default Cors settings ctor
    /// </summary>
    /// <param name="validateOrigin">Validate origin callback. <p/> Always true by default</param>
    /// <param name="methods">List of HTTP method supporting by CORS. <p/>
    /// GET, POST, PUT, HEAD, DELETE, PATCH by default</param>
    /// <param name="continuePreflight">Should continue routing on preflight requests? <p/>
    /// False by default</param>
    /// <param name="code">Code using to response on preflight requests. <p/>
    /// 204 NoContent by default</param>
    /// <param name="useCredentials">Should use credentials? <p/>
    /// False by default</param>
    /// <param name="allowedHeaders">List of allowed headers <p/>
    /// Not sending it by default, list is empty</param>
    /// <param name="exposedHeaders">List of exposed headers <p/>
    /// Not sending it by default, list is empty</param>
    public CorsSettings(Func<Context, Task<bool>>? validateOrigin,
        HttpMethod[]? methods,
        bool continuePreflight,
        HttpStatusCode? code,
        uint? maxAge,
        bool useCredentials,
        string[]? allowedHeaders,
        string[]? exposedHeaders)
    {
        ValidateOrigin = validateOrigin ?? (_ => Task.FromResult(true));
        Methods = methods ??
        [
            HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Head, HttpMethod.Delete,
            HttpMethodExtensions.Patch
        ];
        ContinuePreflight = continuePreflight;
        Code = code ?? HttpStatusCode.NoContent;
        UseCredentials = useCredentials;
        AllowedHeaders = allowedHeaders ?? [];
        ExposedHeaders = exposedHeaders ?? [];
        MaxAge = maxAge;
    }

    public CorsSettings(
        string[]? origins = default,
        HttpMethod[]? methods = default,
        bool continuePreflight = false,
        HttpStatusCode code = HttpStatusCode.NoContent,
        uint? maxAge = null,
        bool useCredentials = false,
        string[]? allowedHeaders = default,
        string[]? exposedHeaders = default
    )
        : this(Create(origins),
            methods,
            continuePreflight,
            code,
            maxAge,
            useCredentials,
            allowedHeaders,
            exposedHeaders)
    {
    }

    public CorsSettings(
        Regex[]? matchers = default,
        HttpMethod[]? methods = default,
        bool continuePreflight = false,
        HttpStatusCode code = HttpStatusCode.NoContent,
        uint? maxAge = null,
        bool useCredentials = false,
        string[]? allowedHeaders = default,
        string[]? exposedHeaders = default
    )
        : this(Create(matchers),
            methods,
            continuePreflight,
            code,
            maxAge,
            useCredentials,
            allowedHeaders,
            exposedHeaders)
    {
    }

    private static Func<Context, Task<bool>> Create(string[]? origins)
    {
        if (origins?.Any() != true)
        {
            return _ => Task.FromResult(true);
        }

        return ctx =>
        {
            var origin = ctx.ClientHeaders["Origin"];
            return Task.FromResult(origins.Contains(origin));
        };
    }

    private static Func<Context, Task<bool>> Create(Regex[]? matchers)
    {
        if (matchers?.Any() != true)
        {
            return _ => Task.FromResult(true);
        }

        return ctx =>
        {
            var origin = ctx.ClientHeaders["Origin"];
            return Task.FromResult(matchers.Any(x => x.IsMatch(origin)));
        };
    }

    public HttpMethod[] Methods { get; }
    public string[] AllowedHeaders { get; }
    public string[] ExposedHeaders { get; }
    public bool ContinuePreflight { get; }
    public bool UseCredentials { get; }
    public HttpStatusCode Code { get; }
    public Func<Context, Task<bool>> ValidateOrigin { get; }
    public uint? MaxAge { get;}
}