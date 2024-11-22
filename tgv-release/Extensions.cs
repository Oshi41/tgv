namespace tgv_release;

public static class Extensions
{
    public static Version CreateRelease(this Version version) => new(version.Major + 1, 0, 0, 0);
    public static Version CreateMinor(this Version version) => new(version.Major, version.Minor + 1, 0, 0);

    public static Version CreateBuild(this Version version) => new(version.Major, version.Minor, version.Build + 1, 0);

    public static Version CreateRevision(this Version version) => new(version.Major, version.Minor, version.Build, version.Revision + 1);
}