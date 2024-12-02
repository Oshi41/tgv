using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace tgv_server_kestrel;

public class KestrelSettings
{
    public X509Certificate2? Certificate { get; set; }
    public HttpProtocols Protocols { get; set; } = HttpProtocols.Http1;
}