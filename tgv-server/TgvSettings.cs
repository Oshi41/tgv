using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace tgv_server;

public class TgvSettings
{
    /// <summary>
    /// Should automatocally add server header?
    /// </summary>
    public bool AddServerHeader { get; set; } = true;
    
    /// <summary>
    /// Possible HTTPS certificate.
    /// Otherwise will use regular HTTP connection
    /// </summary>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>
    /// Default SSL protocol
    /// </summary>
    public SslProtocols Protocols { get; set; } = SslProtocols.None;

    /// <summary>
    /// Callback method to validate server certificates during authentication.
    /// </summary>
    public RemoteCertificateValidationCallback? CertificateValidation { get; set; } 
}