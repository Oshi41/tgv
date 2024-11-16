using System.Net;
using Flurl.Http;
using tgv;
using tgv.imp;

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

public class AppTests
{
    private readonly List<User> _users = new();
    private App _app;

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

        _app = new App();
        _app.After((context, next, exception) =>
        {
            if (exception != null)
            {
                context.Send(HttpStatusCode.InternalServerError);
            }
            else
            {
                context.Send(HttpStatusCode.NotFound);
            }

            return Task.CompletedTask;
        });

        var user = new Router("users");
        _app.Use(user);
        user.Get("", (context, next, exception) =>
        {
            context.Json(_users.Select(x => x.Id).OrderBy(x => x));
            return Task.CompletedTask;
        });
        user.Get("/:user", (context, next, exception) =>
        {
            var id = context.Parameters["user"];
            var user = _users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                context.Send(HttpStatusCode.NotFound);
            else
            {
                context.Json(new { user.FullName });
            }

            return Task.CompletedTask;
        });
        user.Delete("/:user", (context, next, exception) =>
        {
            var id = context.Parameters["user"];
            var user = _users.FirstOrDefault(x => x.Id == id);
            if (user == null)
                context.Send(HttpStatusCode.NotFound);
            else
            {
                _users.Remove(user);
                context.Send(HttpStatusCode.OK);
            }

            return Task.CompletedTask;
        });
        user.Post("", async (context, next, exception) =>
        {
            var user = await context.Body<User>();
            if (_users.Any(x => x.Id == user.Id))
                throw context.Throw(HttpStatusCode.BadRequest, "User already exists");

            _users.Add(user);
            await context.Send(HttpStatusCode.OK);
        });

        var details = new Router("/:user");
        user.Use(details);
        details.Get("", (context, next, exception) =>
        {
            var user = _users.FirstOrDefault(x => x.Id == context.Parameters["user"]);
            if (user == null)
                throw context.Throw(HttpStatusCode.NotFound, "User does not exist");

            context.Json(user.Details);
            return Task.CompletedTask;
        });

        _app.Start(TestUtils.RandPort());
    }

    [TearDown]
    public void Teardown()
    {
        _app?.Stop();
    }

    [Test]
    public async Task AllUsers()
    {
        var expected = _users.Select(x => x.Id).OrderBy(x => x).ToList();
        var ids = await (_app.RunningUrl + "users").GetJsonAsync<string[]>();
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
            var ids = await (_app.RunningUrl + "users").GetJsonAsync<string[]>();
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

            var result = await (_app.RunningUrl + "users").PostJsonAsync(user);
            Assert.That(TestUtils.IsSuccess(result.StatusCode));
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