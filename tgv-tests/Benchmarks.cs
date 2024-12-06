using System.Net;
using Flurl.Http;
using Microsoft.AspNetCore.Builder;

namespace tgv_tests;

[TestFixture, TestFixtureSource(typeof(Benchmarks), nameof(Cases))]
public class Benchmarks
{
    public static readonly string[] Cases = ["asp", "tgv", "simple", "netcoreserver"];
    private static readonly string[] Users = Enumerable.Range(0, 20).Reverse().Select(x => "Id_" + x).ToArray();
    private readonly Uri _url;

    public Benchmarks(string srv)
    {
        switch (srv)
        {
            case "asp":
                _url = new("http://localhost:4000/");
                var builder = WebApplication.CreateBuilder();
                var asp = builder.Build();

                asp.MapGet("", () => "Hello world!");
                asp.MapGet("/users", () => Users.OrderBy(x => x));
                asp.Urls.Add(_url.ToString());
                asp.StartAsync().Wait();
                break;
            
            case "simple":
                _url = new("http://localhost:4002/");
                var simple = new TcpSocketListener();
                simple.Start(4002);
                break;
            
            case "netcoreserver":
                _url = new("http://localhost:4003/");
                var httpserver = new CoreServer(new IPEndPoint(IPAddress.Any, 4003));
                httpserver.Start();
                break;
        }
    }

    [Test]
    public async Task GetHello()
    {
        for (int i = 0; i < 10_000; i++)
        {
            var text = await (_url).AllowHttpStatus("2xx").GetStringAsync();
            Assert.That(text, Is.EqualTo("Hello world!"));
        }
    }

    [TestCase(10)]
    public async Task GetUsers(int count)
    {
        long items = 0;

        await Task.WhenAll(Enumerable.Range(0, count).Select(async _ =>
        {
            var ids = await (_url + "users").GetJsonAsync<string[]>();
            Interlocked.Add(ref items, ids.Length);
        }));

        Assert.That(items, Is.EqualTo(count * 20));
    }
}