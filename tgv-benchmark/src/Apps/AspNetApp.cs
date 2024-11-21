using Microsoft.AspNetCore.Mvc;

namespace tgv_benchmark.Apps;

public class AspNetApp : IApp
{
    private WebApplication _app;

    public AspNetApp()
    {
        var builder = WebApplication.CreateBuilder([]);
        _app = builder.Build();

        var users = Enumerable.Range(0, 20)
            .Select(i => new User(i.ToString()))
            .ToList();

        _app.MapGet("/say_hello", async () => "Hello World!");
        _app.MapGet("/users", async () => users.Select(x => x.Id).Order());
        _app.MapGet("/users/{id}", async ([FromRoute] string id) =>
        {
            var user = users.FirstOrDefault(x => x.Id == id);
            return (object?)user ?? Results.NotFound();
        });
        _app.MapPost("/users", async (User? user) =>
        {
            if (user == null) return Results.BadRequest();
            if (users.Any(x => x.Id == user.Id))
            {
                return Results.Problem("already exists",
                    statusCode: 404);
            }

            users.Add(user);
            return Results.Ok();
        });
        _app.MapPatch("/users", async (User? user) =>
        {
            if (user == null) return Results.BadRequest();
            var source = users.FirstOrDefault(x => x.Id == user.Id);
            if (source == null) return Results.NotFound();

            users.Remove(source);
            users.Add(user);
            return Results.Ok();
        });
        _app.MapDelete("/users/{id}", async ([FromRoute] string id) =>
        {
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user == null) return Results.NotFound();
            users.Remove(user);
            return Results.Ok();
        });

        _app.Urls.Add("http://localhost:7000");
    }

    public void Run()
    {
        _app.Start();
    }
}