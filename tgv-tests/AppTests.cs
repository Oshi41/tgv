using System.Net;
using Flurl.Http;
using tgv_core.imp;
using tgv;

namespace tgv_tests;

class Details
{
    public int Age { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
}

class User
{
    public string Id { get; set; }
    public string FullName => $"{Details?.Name} {Details?.Surname}";
    public Details Details { get; set; }
}

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

        var user = new Router("users");
        _app.Use(user);
        user.Get("", (context, _, _) => 
            context.Json(_users.Select(x => x.Id).OrderBy(x => x)));
        
        user.Get("/:user", (context, _, _) =>
        {
            var id = context.Parameters["user"];
            var user = _users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                return context.Send(HttpStatusCode.NotFound);
            
            return context.Json(new { user.FullName });
        });
        user.Delete("/:user", (context, _, _) =>
        {
            var id = context.Parameters["user"];
            var user = _users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                return context.Send(HttpStatusCode.NotFound);
            _users.Remove(user);
            return context.Send(HttpStatusCode.OK);
        });
        user.Post("", async (context, _, _) =>
        {
            var user = await context.Body<User>();
            if (_users.Any(x => x.Id == user.Id))
                throw context.Throw(HttpStatusCode.BadRequest, "User already exists");

            _users.Add(user);
            await context.Send(HttpStatusCode.OK);
        });

        var details = new Router("/:user/details");
        user.Use(details);
        details.Get("", (context, _, _) =>
        {
            var user = _users.FirstOrDefault(x => x.Id == context.Parameters["user"]);
            if (user == null)
                throw context.Throw(HttpStatusCode.NotFound, "User does not exist");

            return context.Json(user.Details);
        });
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
        
        ids = await (_app.RunningUrl + "redirect").GetJsonAsync<string[]>();
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

            await (_app.RunningUrl + "users").AllowHttpStatus("2xx").PostJsonAsync(user);
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