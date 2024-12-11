using System.Text;
using Flurl.Http;
using tgv_core.imp;
using tgv_large_files;
using tgv_serve_static;
using tgv_server;
using tgv;

namespace tgv_tests;

[TestFixture, Ignore("WIP")]
public class ServerLargeFile
{
    private string _content;
    private string _folder;
    private App _app;

    [SetUp]
    public void Setup()
    {
        _content = string.Join(Environment.NewLine, Enumerable.Range(0, 100).Select(x => "string_" + x));
        _folder = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}");
        Directory.CreateDirectory(_folder);
        var filepath = Path.Combine(_folder, $"static_{Guid.NewGuid()}.txt");
        File.WriteAllText(filepath, _content);

        _app = new App(h => new TgvServer(new TgvSettings(), h, new Logger()));
        _app.ServeFile("/file", filepath, 4096);
        _app.Start(TestUtils.RandPort()).Wait();
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);

        _app.Stop();
        _content = string.Empty;
    }

    [TestCase(4096)]
    [TestCase(1000)]
    [TestCase(100)]
    public async Task DownloadFile(int chunk)
    {
        var client = _app.CreateAgent(chunk + "");

        var req = await client.Request("file")
            .AllowHttpStatus("2xx")
            .HeadAsync();

        Assert.That(long.TryParse(req.Headers.FirstOrDefault("Content-Length"), out var length));
        Assert.That(req.Headers.FirstOrDefault("Accept-Ranges"), Is.EqualTo("bytes"));

        var filepath = Path.Combine(_folder, $"download_{Guid.NewGuid()}.txt");
        File.WriteAllText(filepath, "");

        for (int i = 0; i < length; i += chunk)
        {
            var str = await client.Request("file")
                .WithHeader("Range", $"bytes={i}-{i + chunk}")
                .WithTimeout(7)
                .AllowHttpStatus("206")
                .GetStringAsync();

            File.AppendAllText(filepath, str);
        }

        Assert.That(File.ReadAllText(filepath), Is.EqualTo(_content));
    }
}