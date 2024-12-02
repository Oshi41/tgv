using System.Reflection;

namespace tgv_tests;

[TestFixture]
public class TgvTestUtils
{
    [TestCase("/users/all")]
    [TestCase("http://example.com/users/all")]
    public void Parse(string url)
    {
        var wasParsed = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri);
        Assert.That(wasParsed);
    }

    [TestCase(1, 0, "1.0")]
    [TestCase(1, 1, "1.1")]
    [TestCase(2, 0, "2.0")]
    [TestCase(3, 0, "3.0")]
    public void VersionToString(int major, int minor, string expected)
    {
        Assert.That(new Version(major, minor).ToString(), Is.EqualTo(expected));
    }
}