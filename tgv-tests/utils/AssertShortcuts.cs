namespace tgv_tests.utils;

public static class AssertShortcuts
{
    public static void AreEquals(object left, object right, string? message = null)
        => Assert.That(left, Is.EqualTo(right), message);
    
    public static void AreNotEquals(object left, object right, string? message = null)
        => Assert.That(left, Is.Not.EqualTo(right), message);
    
    public static void AreGreater(object left, object right, string? message = null)
        => Assert.That(left, Is.GreaterThan(right), message);
    
    public static void AreGreaterOrEqual(object left, object right, string? message = null) 
        => Assert.That(left, Is.GreaterThanOrEqualTo(right), message);
}