using System.Diagnostics;
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
            CommitFiles($"release v{IncrementVersion(args)}");
        }

        if (args.Contains("--release"))
        {
            Run(info =>
            {
                info.FileName = "dotnet";
                info.Arguments = $"clean tgv.sln -c Release";
            });
            Run(info =>
            {
                info.FileName = "dotnet";
                info.Arguments = $"build tgv.sln -c Release";
            });
        }

        Console.WriteLine("DONE");
    }

    private static void ShowHelp(string? additional = null)
    {
        if (!string.IsNullOrEmpty(additional))
            Console.WriteLine(additional);
        
        Console.WriteLine("Usage: tgv_release.exe --increment [--major] [--minor] [--revision]");
        Console.WriteLine("Usage: tgv_release.exe --release");
        Console.WriteLine("Usage: tgv_release.exe [--help] [-?]");
    }

    private static Version IncrementVersion(string[] args)
    {
        var settingsPath = Path.Combine(GetCurrentDirectoryPath(), "settings.json");
        var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsPath));
        
        settings!.Version = args.Contains("--major") ? settings.Version.CreateRelease() 
            : args.Contains("--minor") ? settings.Version.CreateMinor()
            : args.Contains("--revision") ? settings.Version.CreateRevision()
            : settings.Version.CreateBuild();
        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        MarkChanged(settingsPath);
        
        foreach (var projectFile in Directory.GetFiles(Path.GetDirectoryName(GetCurrentDirectoryPath()), "*.csproj", SearchOption.AllDirectories))
        {
            var doc = XDocument.Load(projectFile);
            var changed = false;
            using var _ = Disposable.Create(() =>
            {
                if (changed)
                {
                    doc.Save(projectFile);
                    MarkChanged(projectFile);
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

        return settings.Version;
    }

    private static void MarkChanged(string path)
    {
        Run(info =>
        {
            info.FileName = "git";
            info.Arguments = $"add {Path.GetRelativePath(info.WorkingDirectory, path)}";
        });
    }

    private static void CommitFiles(string message)
    {
        Run(info =>
        {
            info.FileName = "git";
            info.Arguments = $"checkout";
        });
        Run(info =>
        {
            info.FileName = "git";
            info.ArgumentList.Add("commit");
            info.ArgumentList.Add($"-m \"{message}\"");
        });
    }

    private static void Run(Action<ProcessStartInfo> customize)
    {
        var dir = Path.GetFullPath(Path.Combine(GetCurrentDirectoryPath(), ".."));
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = dir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        customize?.Invoke(startInfo);
        Run(startInfo);
    }

    private static void Run(ProcessStartInfo info)
    {
        using var process = Process.Start(info);
        process!.Start();
        
        process.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput());
        process.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());
        
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var locker = Path.Combine(GetCurrentDirectoryPath(), "..", ".git", "index.lock");
            if (File.Exists(locker))
            {
                Thread.Sleep(200);
                Run(info);
            }
            
            throw new Exception($"Failed to run command {info.FileName} {info.Arguments}");
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