namespace sharp_express.middleware.static_serve;

public class StaticFilesConfig
{
    public bool FallThrough { get; set; } = true;
    public string SourceDirectory { get; set; }
}