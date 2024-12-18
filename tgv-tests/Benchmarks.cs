using System.Net;
using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using tgv_core.imp;
using tgv_server;
using tgv;

namespace tgv_tests;

[TestFixture, TestFixtureSource(typeof(Benchmarks), nameof(Cases))]
public class Benchmarks
{
    public static readonly string[] Cases = ["asp", "tgv"];
    private static readonly string[] Users = Enumerable.Range(0, 20).Reverse().Select(x => "Id_" + x).ToArray();
    private static int _port = 4000;
    
    private readonly Uri _url;
    private readonly List<Action> _teardown = new();

    public Benchmarks(string srv)
    {
        var port = Interlocked.Increment(ref _port);
        _url = new("http://localhost:" + port + "/");
        
        switch (srv)
        {
            case "asp":
                var builder = WebApplication.CreateBuilder();
                var asp = builder.Build();

                asp.MapGet("", () => "Hello world!");
                asp.MapGet("/users", () => Users.OrderBy(x => x));
                asp.Urls.Add(_url.ToString());
                asp.StartAsync().Wait();
                _teardown.Add(() =>  asp.DisposeAsync());
                break;
            
            case "tgv":
                var app = new App(new TgvServer(new TgvSettings()));
                app.Get("/users", (ctx, _, _) => ctx.Json(Users.OrderBy(x => x)));
                app.Get("", (context, _, _) => context.Text("Hello world!"));
                app.Start(port).Wait();
                _teardown.Add(() =>  app.Stop());
                break;
        }
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var action in _teardown)
        {
            action();
        }
    }

    [TestCase(10_000)]
    public async Task GetHello(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var text = await (_url).AllowHttpStatus("2xx").GetStringAsync();
            Assert.That(text, Is.EqualTo("Hello world!"));
        }
    }

    [TestCase(10_000)]
    public async Task GetUsers(int amount)
    {
        long items = 0;
        
        for (int i = 0; i < amount; i++)
        {
            var ids = await (_url + "users").GetJsonAsync<string[]>();
            items += ids.Length;
        }

        Assert.That(items, Is.EqualTo(amount * 20));
    }
}