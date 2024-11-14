namespace charsp_express_serve_static;

public class StaticFilesConfig
{
    public bool FallThrough { get; set; } = true;
    public string SourceDirectory { get; set; }
}