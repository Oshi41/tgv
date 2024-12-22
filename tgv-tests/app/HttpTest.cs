using System.Diagnostics;
using System.Net;
using System.Text;
using Flurl.Http;
using tgv_server;
using tgv_session;
using tgv;

namespace tgv_tests.app;

[TestFixture]
public class HttpTest
{
    protected string _dir;
    protected readonly TgvSettings _settings;
    protected readonly List<User> _users = new();
    protected App _app;
    protected IFlurlClient _client;

    public HttpTest() : this(new TgvSettings())
    {
    }

    protected HttpTest(TgvSettings settings)
    {
        _settings = settings;
    }

    [SetUp]
    public void Setup()
    {
        Directory.CreateDirectory(_dir = Path.Join(Path.GetTempPath(), $"tgv_{Guid.NewGuid()}"));

        var rand = new Random();

        for (int i = 0; i < 20; i++)
        {
            _users.Add(new User
            {
                Id = $"User_{i}",
                Details = new Details
                {
                    Surname = $"Surname_{i}",
                    Name = $"Name_{i}",
                    Age = rand.Next(0, 100),
                }
            });
        }

        _app = new App(new TgvServer(_settings));
        _app.Use(TestUtils.CreateSimpleCRUD(_users));
        _app.Start(TestUtils.RandPort()).Wait();

        _client = _app.CreateAgent(Guid.NewGuid().ToString());
    }

    [TearDown]
    public async Task Teardown()
    {
        while (true)
        {
            try
            {
                if (Directory.Exists(_dir))
                    Directory.Delete(_dir, true);
                
                break;
            }
            catch (Exception e)
            {
                // ignore. repeat
                await Task.Delay(200);
            }
        }

        _users?.Clear();
        _app?.Stop();
        _client?.Dispose();
    }

    [Test(Description = "Retrieving all the users")]
    public async Task GetUsers()
    {
        string[] ids;
        var expected = _users.Select(x => x.Id).OrderBy(x => x).ToList();
        ids = await _client.Request("users").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));

        ids = await _client.Request("users/redirected").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));
    }

    [Test(Description = "Requesting every user and check it's full name")]
    public async Task GetUser()
    {
        await Task.WhenAll(_users.Select(async user =>
        {
            var userProps = await _client.Request("users/" + user.Id)
                .GetJsonAsync<Dictionary<string, string>>();

            Assert.That(userProps[nameof(user.FullName)], Is.EqualTo(user.FullName));
        }));
    }

    [Test(Description = "Deleting every user")]
    public async Task Delete()
    {
        var count = _users.Count;

        async Task CheckCount(int amount, string? deleted = null)
        {
            var ids = await _client.Request("users").GetJsonAsync<string[]>();
            Assert.That(ids.Length, Is.EqualTo(amount));
            if (deleted != null)
                Assert.That(ids.Contains(deleted), Is.False);
        }

        await CheckCount(count);

        // collection will be modified
        foreach (var user in _users.ToList())
        {
            var result = await _client.Request("users/" + user.Id).DeleteAsync();
            Assert.That(TestUtils.IsSuccess(result.StatusCode));
            count--;

            await CheckCount(count, user.Id);
        }
    }

    [Test(Description = "Creating a new users")]
    public async Task CreateUser()
    {
        async Task Contains(string id)
        {
            var ids = await _client.Request("users").AllowHttpStatus("2xx").GetJsonAsync<string[]>();
            Assert.That(ids.Contains(id));
        }

        var count = _users.Count;

        for (int i = 0; i < 10; i++)
        {
            var user = new User
            {
                Id = $"Some_New_User_{i}",
                Details = new Details
                {
                    Name = "Name_" + i,
                    Surname = $"Surname_{i}",
                    Age = i
                }
            };

            await _client.Request("users").AllowHttpStatus("2xx").PutJsonAsync(user);
            count++;
            await Contains(user.Id);
            Assert.That(_users.Count, Is.EqualTo(count));
        }
    }

    public static string[] NotExistedUsers =
    [
        "unknownName",
        "123123!@#$%^&*(WERTYUIODFGHJKLXCVBNM<.xercdftvybghujim,kp;",
        "*",
        ":user",
        "",
    ];

    [Test(Description = "Requesting not existed user")]
    public async Task GetUser_NotExists([ValueSource(nameof(NotExistedUsers))] string id)
    {
        _ = await _client.Request("users/" + id)
            .AllowHttpStatus("4xx")
            .GetAsync();
    }

    [Test(Description = "Requesting not existed user")]
    public async Task Delete_NotExists([ValueSource(nameof(NotExistedUsers))] string id)
    {
        _ = await _client.Request("users/" + id)
            .AllowHttpStatus("4xx")
            .DeleteAsync();
    }

    [Test(Description = "Creating a new user with existing ID")]
    public async Task CreateUser_SameIdAlreadyExists()
    {
        var count = _users.Count;
        foreach (var user in _users)
        {
            // request should fail due to same ID
            await _client.Request("users").AllowHttpStatus("4xx").PutJsonAsync(user);
            // requesting all the users
            var ids = await _client.Request("users").AllowHttpStatus("2xx").GetJsonAsync<string[]>();
            // amount shuold not change
            Assert.That(ids.Length, Is.EqualTo(count));
        }
    }

    [Test(Description = "Download file as stream")]
    public async Task DownloadFile()
    {
        var fileName = Path.Join(_dir, $"served_{Guid.NewGuid()}.txt");
        using (var fs = File.OpenWrite(fileName))
        {
            var line = Encoding.UTF8.GetBytes(string.Join("\r\n", Enumerable.Repeat("1234567890qwerty", 100_000)));
            Enumerable.Repeat('\0', 1000).AsParallel().ForAll(_ => fs.Write(line, 0, line.Length));
            fs.Flush();
            fs.Close();
        }

        var info = new FileInfo(fileName);
        // file >= 10MB
        Assert.That(info.Length, Is.GreaterThan(Math.Pow(2, 20) * 10));
        Console.WriteLine("Served file size: {0:F}MB", info.Length / Math.Pow(2, 20));

        _app.Get("/download_file", async (ctx, _, _) =>
        {
            await using var stream = File.OpenRead(fileName);
            await ctx.SendFile(stream, "download.txt");
        });

        var downloaded = $"download_{Guid.NewGuid()}.txt";
        var downloadedFilePath = Path.Join(_dir, downloaded);

        var sw = Stopwatch.StartNew();
        await _client.Request("download_file")
            .AllowHttpStatus("2xx")
            .DownloadFileAsync(_dir, downloaded);
        sw.Stop();
        var seconds = sw.ElapsedMilliseconds / 1000.0;
        var mbs = info.Length / Math.Pow(2, 20);
        var speed = mbs / seconds;
        Console.WriteLine("Transfer speed: {0:F}MB/s", mbs / seconds);

        if (speed < 50) Assert.Warn($"Download file speed was too slow: {speed:F} MB/s");
        if (speed < 5) Assert.Fail($"Download file speed was way too slow: {speed:F} MB/s");
        
        Assert.That(File.Exists(downloadedFilePath), Is.True);
    }

    [Test(Description = "Simple form handling")]
    public async Task UrlEncodedForm()
    {
        _app.Post("/calc", (ctx, _, _) =>
        {
            var x = double.Parse(ctx.Form["x"]);
            var y = double.Parse(ctx.Form["y"]);
            var result = ctx.Form["operand"] switch
            {
                "+" => x + y,
                "-" => x - y,
                "/" => x / y,
                "*" => x * y,
            };
            
            
            return ctx.Text(result.ToString());
        });
        
        var f = 5.2;
        var s = 3.123;
        var list = new List<string[]>()
        {
            new[] { f.ToString(), "+", s.ToString(), (f + s).ToString() },
            new[] { f.ToString(), "-", s.ToString(), (f - s).ToString() },
            new[] { f.ToString(), "*", s.ToString(), (f * s).ToString() },
            new[] { f.ToString(), "/", s.ToString(), (f / s).ToString() },
        };
        
        foreach (var arr in list)
        {
            var x = arr[0];
            var operand = arr[1];
            var y = arr[2];
            var expected = arr[3];

            var resp = await _client.Request("calc")
                .PostUrlEncodedAsync(new { x, operand, y, });
            var res = await resp.GetStringAsync();
            Assert.That(res, Is.EqualTo(expected));
        }
    }
}