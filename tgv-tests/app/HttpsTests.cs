using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Flurl.Http;
using tgv_server;

namespace tgv_tests.app;

public class HttpsTests : HttpTest
{
    public HttpsTests() : base(new TgvSettings
    {
        Certificate = TestUtils.MakeDebugCert(),
        Protocols = SslProtocols.None,
        CertificateValidation = (_, _, _, _) => true,
        AddServerHeader = true,
    })
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
        TestUtils.SetupFlurlClient(_settings);
    }
}