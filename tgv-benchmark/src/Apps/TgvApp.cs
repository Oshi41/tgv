using System.Net;
using Newtonsoft.Json;
using tgv_core.api;
using tgv_kestrel_server;
using tgv_watson_server;
using tgv;

namespace tgv_benchmark.Apps;

public class TgvApp : IApp
{
    private readonly App _app;

    public TgvApp(string[] args)
    {
        Func<ServerHandler, IServer> createServer = handler =>
        {
            if (args.Contains("watson"))
            {
                return new WatsonServer(handler);
            }

            if (args.Contains("kestrel"))
            {
                return new KestrelServer(handler, _app!.Logger);
            }

            throw new Exception("Unknown server implementation");
        };


        _app = new App(createServer);
        _app.Logger.WriteLog = _ => { };
        var users = Enumerable.Range(0, 20).Select(i => new User(i.ToString())).ToList();

        _app.Get("/say_hello", (ctx, _, _) => ctx.Text("Hello World!"));
        _app.Get("/users", (ctx, _, _) => ctx.Json(users.Select(x => x.Id).Order()));
        _app.Get("/users/:id", async (ctx, _, _) =>
        {
            var id = ctx.Parameters["id"];
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user == null)
            {
                await ctx.Send(HttpStatusCode.NotFound);
                return;
            }

            await ctx.Json(user);
        });
        _app.Post("/users", async (ctx, _, _) =>
        {
            var user = await ctx.Body<User>();
            if (users.Any(x => x.Id == user.Id))
            {
                throw ctx.Throw(HttpStatusCode.BadRequest, "already exists");
            }

            users.Add(user);

            ctx.Logger.Info($"User added: {JsonConvert.SerializeObject(user, Formatting.Indented)}");
            await ctx.Send(HttpStatusCode.OK);
        });
        _app.Delete("/users/:id", async (ctx, _, _) =>
        {
            var id = ctx.Parameters!["id"];
            var user = users.FirstOrDefault(x => x.Id == id);
            if (user == null) throw ctx.Throw(HttpStatusCode.NotFound);

            ctx.Logger.Info($"User deleted: {id}");
            users.Remove(user);
            await ctx.Send(HttpStatusCode.OK);
        });
        _app.Patch("/users", async (ctx, _, _) =>
        {
            var user = await ctx.Body<User>();
            var source = users.FirstOrDefault(x => x.Id == user.Id);
            if (source == null) throw ctx.Throw(HttpStatusCode.NotFound);

            users.Remove(source);
            users.Add(user);
            ctx.Logger.Info($"Old user: {JsonConvert.SerializeObject(source, Formatting.Indented)}");
            ctx.Logger.Info($"Updated user: {JsonConvert.SerializeObject(user, Formatting.Indented)}");
            await ctx.Send(HttpStatusCode.OK);
        });
    }

    public void Run()
    {
        _app.Start(7000).Wait();
    }
}