using System;
using System.Security.Cryptography.X509Certificates;

namespace tgv_server;

public class Settings
{
    public bool AddServerHeader { get; set; } = true;
    public X509Certificate2? Certificate { get; set; }
}