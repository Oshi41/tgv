using System.Security.Cryptography.X509Certificates;

namespace tgv.core;

public class AppConfig
{
    public string ContextPath { get; set; } = "/";
    public string DefaultContentType { get; set; } = "text/plain";
    public bool UseHttps { get; set; }
    public X509Certificate? Certificate { get; set; }
}