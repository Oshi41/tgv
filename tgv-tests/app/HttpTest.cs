using Flurl.Http;
using tgv_core.imp;
using tgv_server;
using tgv;

namespace tgv_tests.app;

[TestFixture]
public class HttpTest
{
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

        _app = new App(x => new TgvServer(_settings, x, new Logger()));
        _app.Use(TestUtils.CreateSimpleCRUD(_users));
        _app.Start(TestUtils.RandPort()).Wait();
        
        _client = _app.CreateAgent(Guid.NewGuid().ToString());
    }

    [TearDown]
    public void Teardown()
    {
        _users.Clear();
        _app.Stop();
        _client.Dispose();
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
}