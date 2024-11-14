using tgv.core;
using tgv.imp;

namespace tgv_tests;

public class RouterTests
{
    [Test]
    public void TestComplicatedHierarchy()
    {
        var calls = new Dictionary<string, List<string>>();
        Handle baseHandler = (context, next, _) =>
        {
            if (!calls.TryGetValue(context.HttpMethod, out var records))
            {
                calls[context.HttpMethod] = records = new List<string>();
            }

            records.Add(context.Url.PathAndQuery);
            next();
            return Task.CompletedTask;
        };

        var root = new Router("*");

        var user = new Router("/user");
        root.Use(user);
        user.Get("/find/:id", baseHandler);
        user.Post("/delete/:id", baseHandler);
        user.Put("/create/:id", baseHandler);

        var details = new Router("/details");
        user.Use(details);
        details.Get("/*/info", baseHandler);
        
        var color = new Router("/color");
        root.Use(color);
        color.Get("/random/twice", baseHandler, baseHandler);
        color.Get("/random/once", baseHandler, (context, next, _) =>
        {
            // not going forward
            return Task.CompletedTask;
        }, baseHandler);

        //R     /user
        //G             /find/:id
        //G             /delele/:id     
        //G             /create/:id
        //
        //R             /details
        //G                     /*/info
        //R     /color
        //G             /random/twice
        //G             /random/once

        var ctx = TestUtils.Create("/user/find/1");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["GET"].Contains("/user/find/1"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(1));

        ctx = TestUtils.Create("/user/delete/123", "POST");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["POST"].Contains("/user/delete/123"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(2));

        ctx = TestUtils.Create("/user/create/567567", "PUT");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["PUT"].Contains("/user/create/567567"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(3));

        ctx = TestUtils.Create("/user/details/some/info");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["GET"].Contains("/user/details/some/info"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(4));

        ctx = TestUtils.Create("/user/details/some/more/path/segments/info");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["GET"].Contains("/user/details/some/more/path/segments/info"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(5));
        
        ctx = TestUtils.Create("/color/random/once");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["GET"].Contains("/color/random/once"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(6));
        
        ctx = TestUtils.Create("/color/random/twice");
        Assert.That(root.Match(ctx));
        root.Handler(ctx, () => { }).Wait();
        Assert.That(calls["GET"].Contains("/color/random/twice"));
        Assert.That(calls.Values.Sum(x => x.Count), Is.EqualTo(8));
    }
}