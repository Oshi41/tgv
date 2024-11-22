using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Newtonsoft.Json;

namespace tgv_kestrel_server;

public class KestrelSettings
{
    HttpsConnectionAdapterOptions Https { get; } = new();
    KestrelServerLimits Limits { get; } = new();

    internal KestrelServerOptions Convert()
    {
        var result = new KestrelServerOptions();
        result.ConfigureHttpsDefaults(Clone);
        Clone(result.Limits);
        result.AddServerHeader = false;
        result.ApplicationSchedulingMode = SchedulingMode.ThreadPool;
        return result;
    }

    private bool CopyTo<T>(T from, T to)
    {
        try
        {
            var json = JsonConvert.SerializeObject(from, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
            });

            JsonConvert.PopulateObject(json, to);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private void Clone(HttpsConnectionAdapterOptions to)
    {
        CopyTo(Https, to);
        // to.ServerCertificate = Https.ServerCertificate;
        // to.HandshakeTimeout = Https.HandshakeTimeout;
        // to.SslProtocols = Https.SslProtocols;
        // to.CheckCertificateRevocation = Https.CheckCertificateRevocation;
        // to.ClientCertificateMode = Https.ClientCertificateMode;
        // to.ClientCertificateValidation = Https.ClientCertificateValidation;
        // to.ServerCertificateSelector = Https.ServerCertificateSelector;
    }

    private void Clone(KestrelServerLimits to)
    {
        CopyTo(Limits, to);
    }
}