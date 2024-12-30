using System.Net;
using System.Text;
using akasha.api;

namespace tgv_tests.akasha;

public class HttpResponseTests
{
    [Test]
    public void GetHttpWithoutBody()
    {
        var resp = new HttpResponse()
        {
            Protocol = new Version(1, 1),
            Code = HttpStatusCode.OK,
            Headers = new(),
            Cookies = new(),
            Body = new BufferStream(),
        };
        resp.Headers["Host"] = "www.test.com";
        resp.Headers["Content-Type"] = "application/json";
        resp.Headers["Location"] = "http://example.com/users/123";
        resp.Headers["User-Agent"] = "Mozilla/5.0";
        resp.Headers["Custom-Header"] = "Some data";
        
        resp.Cookies.Add(new Cookie("simple", "simple_value"));
        resp.Cookies.Add(new Cookie("complicated", "complicated_value")
        {
            Secure = true,
            HttpOnly = true,
        });
        
        var http = Encoding.UTF8.GetString(resp.GetHttpWithoutBody());
        
        var expected = @"HTTP/1.1 200 OK
Host: www.test.com
Content-Type: application/json
Location: http://example.com/users/123
User-Agent: Mozilla/5.0
Custom-Header: Some data
Set-Cookie: simple=simple_value
Set-Cookie: complicated=complicated_value; HttpOnly; Secure

";
        
        AreEquals(expected, http);
    }
}