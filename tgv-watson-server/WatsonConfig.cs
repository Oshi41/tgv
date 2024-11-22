using WatsonWebserver.Core;

namespace tgv_watson_server;

public class WatsonConfig
{
    public WebserverSettings.IOSettings Io { get; set; } = new();
    public WebserverSettings.SslSettings Ssl { get; set; } = new();
    public WebserverSettings.HeaderSettings Headers { get; set; } = new();
    public WebserverSettings.DebugSettings Debug { get; set; } = new();
    public AccessControlManager Access { get; set; } = new();

    internal WebserverSettings Convert()
    {
        return new WebserverSettings
        {
            Ssl = Ssl,
            Headers = Headers,
            Debug = Debug,
            AccessControl = Access,
        };
    }
}