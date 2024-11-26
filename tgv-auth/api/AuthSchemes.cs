namespace tgv_auth.api;

public enum AuthSchemes
{
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7617
    /// </summary>
    Basic,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc6750
    /// </summary>
    Bearer,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7616
    /// </summary>
    Digest,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7486
    /// </summary>
    Hoba,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc8120
    /// </summary>
    Mutal,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc8120
    /// </summary>
    Negotiate,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc8120
    /// </summary>
    Ntlm,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc8292
    /// </summary>
    Vapid,
    
    /// <summary>
    /// https://datatracker.ietf.org/doc/html/rfc7804
    /// </summary>
    Scram,
    
    /// <summary>
    /// https://docs.aws.amazon.com/AmazonS3/latest/API/sigv4-auth-using-authorization-header.html
    /// </summary>
    Aws,
}