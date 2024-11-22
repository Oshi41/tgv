using Newtonsoft.Json;

namespace tgv_release;

public class Settings
{
    [JsonProperty("version")] public Version Version { get; set; }
}