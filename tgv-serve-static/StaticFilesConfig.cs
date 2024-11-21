namespace tgv_serve_static;

public class StaticFilesConfig
{
    public StaticFilesConfig(string sourceDirectory)
    {
        SourceDirectory = sourceDirectory;
    }

    public bool FallThrough { get; set; } = true;
    public string SourceDirectory { get; }
}