using System.Text;
using akasha.parser;

namespace tgv_tests.akasha;

public class HttpParserTests
{
    [Test]
    public async Task Works()
    {
        var htmlBody = """
                   1234567800
                   asfdadsfhwrythbvavaopkembviljatngtblihtnbvaksdmvoihaertng
                   adfvopjaedfnmvpoa,dopcjmfvipadfvipsfnmvjisaedfmc kopDSCmcoiaufnvui0aejvipamerv
                   asopjncvaodfivnadiofnvaohuidfnvoandfafdoivafdhn
                   """;
        var htmlHeader = """
                         POST /some/path HTTP/1.1
                         Host: developer.mozilla.org
                         Content-Type: plain/text
                         Location: http://example.com/users/123
                         User-Agent: Mozilla/5.0
                         Custom-Header: Some data
                         Cookie: simple=simple_value
                         Cookie: complicated=complicated_value
                         """;

        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes($"{htmlHeader}\r\n\r\n{htmlBody}\r\n\r\n"));
        stream.Seek(0, SeekOrigin.Begin);

        var request = await stream.ParseHttpRequest();

        AreEquals(request.Method, HttpMethod.Post);
        AreEquals(request.Uri, "/some/path");
        AreEquals(request.Protocol, new Version(1, 1));

        Assert.That(request.Headers, Is.Not.Null.And.Not.Empty);
        AreEquals(request.Headers["Host"], "developer.mozilla.org");
        AreEquals(request.Headers["Content-Type"], "plain/text");
        AreEquals(request.Headers["Location"], "http://example.com/users/123");
        AreEquals(request.Headers["User-Agent"], "Mozilla/5.0");
        AreEquals(request.Headers["Custom-Header"], "Some data");

        Assert.That(request.Cookies, Is.Not.Null.And.Not.Empty);

        var cookie = request.Cookies.First(x => x.Name == "simple");
        AreEquals(cookie.Value, "simple_value");

        cookie = request.Cookies.First(x => x.Name == "complicated");
        AreEquals(cookie.Value, "complicated_value");

        Assert.That(request.Body, Is.Not.Null);
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        AreEquals(body, htmlBody);
    }
}