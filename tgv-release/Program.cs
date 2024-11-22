using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using tgv_core.imp;

namespace tgv_release;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-?"))
        {
            ShowHelp();
            return;
        }

        if (args.Contains("--increment"))
        {
            IncrementVersion(args);
            return;
        }
        
        ShowHelp("No command founded");
    }

    private static void ShowHelp(string? additional = null)
    {
        if (!string.IsNullOrEmpty(additional))
            Console.WriteLine(additional);
        
        Console.WriteLine("Usage: tgv_release.exe --increment [--major] [--minor] [--revision]");
        Console.WriteLine("Usage: tgv_release.exe [--help] [-?]");
    }

    private static void IncrementVersion(string[] args)
    {
        var settingsPath = Path.Combine(GetCurrentDirectoryPath(), "settings.json");
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
        settings!.Version = args.Contains("--major") ? settings.Version.CreateRelease() 
            : args.Contains("--minor") ? settings.Version.CreateMinor()
            : args.Contains("--revision") ? settings.Version.CreateRevision()
            : settings.Version.CreateBuild();
        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        
        foreach (var projectFile in Directory.GetFiles(Path.GetDirectoryName(GetCurrentDirectoryPath()), "*.csproj", SearchOption.AllDirectories))
        {
            var doc = XDocument.Load(projectFile);
            var changed = false;
            using var _ = Disposable.Create(() =>
            {
                if (changed)
                {
                    doc.Save(projectFile);
                    Console.WriteLine($"Project file changed: {projectFile}");
                }
            });
            
            Console.WriteLine($"Project file read: {projectFile}");
            foreach (var nodeName in new [] {"Version", "AssemblyVersion", "FileVersion"})
            {
                var node = doc.XPathSelectElement($"/Project/PropertyGroup/{nodeName}");
                if (node != null)
                {
                    node.SetValue(settings.Version.ToString());
                    changed = true;
                }
            }
        }
    }

    private static string GetCurrentDirectoryPath([CallerFilePath] string? path = null)
    {
        path = Path.GetDirectoryName(path);
        return !string.IsNullOrEmpty(path)
            ? path
            : string.Empty;
    }
}