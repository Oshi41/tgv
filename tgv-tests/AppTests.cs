using System.Net;
using Flurl.Http;
using tgv_core.imp;
using tgv;

namespace tgv_tests;


[TestFixtureSource(typeof(Servers), nameof(Servers.AllServers))]
public class AppTests
{
    private readonly List<User> _users = new();
    private readonly App _app;

    public AppTests(Servers.ServerCreationCase fn)
    {
        _app = new App(fn);
        
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

        _app.After((context, _, exception) =>
            context.Send(exception != null ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound));

        _app.Get("/redirect", async (ctx, _, _) => await ctx.Redirect("/users"));

        _app.Use(TestUtils.CreateSimpleCRUD(_users));
    }

    [SetUp]
    public async Task Setup()
    {
        await _app.Start(TestUtils.RandPort());
        Assert.That(_app.RunningUrl, Is.Not.Null.Or.Empty);
    }

    [TearDown]
    public void Teardown()
    {
        _app?.Stop();
    }

    [Test]
    public async Task AllUsers()
    {
        string[] ids;
        var expected = _users.Select(x => x.Id).OrderBy(x => x).ToList();
        ids = await (_app.RunningUrl + "users").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));
        
        ids = await (_app.RunningUrl + "users/redirected").GetJsonAsync<string[]>();
        Assert.That(ids, Is.EqualTo(expected));
    }

    [Test]
    public async Task RequestAllUsers()
    {
        await Task.WhenAll(_users.Select(async user =>
        {
            var userProps = await (_app.RunningUrl + "users/" + user.Id)
                .GetJsonAsync<Dictionary<string, string>>();

            Assert.That(userProps[nameof(user.FullName)], Is.EqualTo(user.FullName));
        }));
    }

    [Test]
    public async Task DeleteAllUsers()
    {
        var count = _users.Count;

        async Task CheckCount(int count, string? deleted = null)
        {
            var ids = await (_app.RunningUrl + "users").GetJsonAsync<string[]>();
            Assert.That(ids.Length, Is.EqualTo(count));
            if (deleted != null)
                Assert.That(ids.Contains(deleted), Is.False);
        }

        await CheckCount(count);

        for (var i = 0; i < _users.Count; i++)
        {
            var user = _users[i];
            var result = await (_app.RunningUrl + "users/" + user.Id).DeleteAsync();
            Assert.That(TestUtils.IsSuccess(result.StatusCode));
            count--;

            await CheckCount(count, user.Id);
        }
    }

    [Test]
    public async Task CreateUser()
    {
        async Task Contains(string id)
        {
            var ids = await (_app.RunningUrl + "users").AllowHttpStatus("2xx").GetJsonAsync<string[]>();
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

            await (_app.RunningUrl + "users").AllowHttpStatus("2xx").PutJsonAsync(user);
            count++;
            await Contains(user.Id);
            Assert.That(_users.Count, Is.EqualTo(count));
        }
    }

    [TestCase("unknownName")]
    [TestCase("123123!@#$%^&*(WERTYUIODFGHJKLXCVBNM<.xercdftvybghujim,kp;")]
    [TestCase("*")]
    [TestCase(":user")]
    [TestCase("")]
    public async Task RequestNotExistedUser(string id)
    {
        try
        {
            _ = await (_app.RunningUrl + "users/" + id).GetJsonAsync<Dictionary<string, string>>();
            Assert.Fail("User does not exist, must fail");
        }
        catch (FlurlHttpException ex)
        {
            // should fail here
        }
    }

    [TestCase("unknownName")]
    [TestCase("123123!@#$%^&*(WERTYUIODFGHJKLXCVBNM<.xercdftvybghujim,kp;")]
    [TestCase("*")]
    [TestCase(":user")]
    [TestCase("")]
    public async Task CannotDeleteNotExistingUser(string id)
    {
        try
        {
            var resp = await (_app.RunningUrl + "users/" + id).DeleteAsync();
            Assert.Fail("User does not exist, must fail");
        }
        catch (FlurlHttpException ex)
        {
            // should fail here
        }
    }
}