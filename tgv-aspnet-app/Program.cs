using Microsoft.AspNetCore.Mvc;
using tgv_aspnet_app;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var users = Enumerable.Range(0, 20)
    .Select(i => new User(i.ToString()))
    .ToList();

app.MapGet("/say_hello", async () => "Hello World!");
app.MapGet("/users", async () => users.Select(x => x.Id).Order());
app.MapGet("/users/{id}", async ([FromRoute] string id) =>
{
    var user = users.FirstOrDefault(x => x.Id == id);
    return (object?)user ?? Results.NotFound();
});
app.MapPost("/users", async (User? user) =>
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
app.MapPatch("/users", async (User? user) =>
{
    if (user == null) return Results.BadRequest();
    var source = users.FirstOrDefault(x => x.Id == user.Id);
    if (source == null) return Results.NotFound();

    users.Remove(source);
    users.Add(user);
    return Results.Ok();
});
app.MapDelete("/users/{id}", async ([FromRoute] string id) =>
{
    var user = users.FirstOrDefault(x => x.Id == id);
    if (user == null) return Results.NotFound();
    users.Remove(user);
    return Results.Ok();
});

app.Run("http://localhost:7000/");