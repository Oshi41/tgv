using System;
using tgv_core.api;

namespace tgv_auth.api;

public interface ICredentialProvider<out T>: IMetricProvider
    where T : ICredentials
{
    AuthSchemes Scheme { get; }

    /// <summary>
    /// Retrieves credentials from HTTP request 
    /// </summary>
    /// <param name="ctx">HTTP request context</param>
    T? GetCredentials(Context ctx);
    
    /// <summary>
    /// Creates challenge header content.
    /// </summary>
    /// <param name="ctx">HTTP request</param>
    /// <param name="ex">Possible exception</param>
    string GetChallenge(Context ctx, Exception? ex);
}