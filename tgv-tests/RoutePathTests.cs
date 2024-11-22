using tgv_common.imp;

namespace tgv_tests;

public class RoutePathTests
{
    [TestCase("/users/1/details")]
    [TestCase("/users/231231/details")]
    [TestCase("/users/231238sudfsd1/details")]
    [TestCase("/users/231238*123/details")]
    [TestCase("/users/231238*123/details?q=1")]
    [TestCase("/users/231238*123/details#q=1")]
    [TestCase("/users/231238*123/details?q=1#q=1")]
    public void Parameters_Match(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "/users/:me/details", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)));
    }

    [TestCase("/1/users/2/details")]
    [TestCase("/domains/users/2/details")]
    [TestCase("/domains/1/2/details")]
    [TestCase("/domains/1/users/details")]
    [TestCase("/domains/1/users/2")]
    [TestCase("/domains/1/users/2/details/some/path")]
    [TestCase("/other/domains/1/users/2/details")]
    public void Parameters_DoNotMatch(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "domains/:domain/users/:me/details", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)), Is.False);
    }

    [TestCase("/some/domains/1/detail")]
    [TestCase("/some/domains/1sdfsdfs/detail")]
    [TestCase("/some/domains/1sldfskjuhgr/detail")]
    [TestCase("/path/some/domains/1sldfskjuhgr/detail")]
    [TestCase("/long/path/some/domains/1sldfskjuhgr/detail")]
    [TestCase("/sooo/long/path/some/domains/1sldfskjuhgr/detail")]
    public void Wildcard_Match(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "*/domains/:id/detail", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)));
    }

    [TestCase("/domains/1/detail")]
    [TestCase("/some/domains/1sdfsdfs/detail/1")]
    [TestCase("/some/domains/1sldfskjuhgr/detail/12")]
    [TestCase("/path/some/domains/1sldfskjuhgr/detail/*")]
    [TestCase("/long/path/some/domains/1sldfskjuhgr/some/detail")]
    [TestCase("/sooo/long/path/some/domains/some/1sldfskjuhgr/detail")]
    public void Wildcard_DoNotMatch(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "*/domains/:id/detail", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)), Is.False);
    }

    [TestCase("/users/1/details/1")]
    [TestCase("/users/1/details/parameter")]
    [TestCase("/users/1/details/value")]
    [TestCase("/users/1/details?detail=value")]
    public void ParameterAsQuery(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "/users/:user/details/:detail", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)));
    }

    [TestCase("/users/1/h/")]
    [TestCase("?q=1")]
    [TestCase("?q=1#h=2")]
    [TestCase("/")]
    [TestCase("/asdasd/asdfsdfsdf/sdfsdf/sdfsd/fsd/fsd/fsdf")]
    public void WildcardOnly(string path)
    {
        var route = new RoutePath(HttpMethod.Get, "*", null, new RouterConfig());
        Assert.That(route.Match(TestUtils.Create(path)));
    }

    [Test]
    public void ParseQuery()
    {
        var route = new RoutePath(HttpMethod.Get, "/users/:user/details/:detail", null, new RouterConfig());

        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 20; j++)
            {
                var dict = route.Parameters(TestUtils.Create($"/users/{i}_str/details/{j}_str"));
                Assert.That(dict["user"], Is.EqualTo($"{i}_str"));
                Assert.That(dict["detail"], Is.EqualTo($"{j}_str"));
            }
        }
    }
}