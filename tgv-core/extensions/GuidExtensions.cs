using System;

namespace tgv_core.extensions;

public static class GuidExtensions
{
    public static Guid CreateId(this long id, DateTime? dateTime = null)
    {
        dateTime ??= DateTime.Now;
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(dateTime.Value.ToFileTimeUtc()), guidData, 8);
        Array.Copy(BitConverter.GetBytes(id), 0, guidData, 8, 8);
        return new Guid(guidData);
    }

    public static (DateTime dateTime, long id) FromId(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var utcTime = BitConverter.ToInt64(bytes, 0);
        var id = BitConverter.ToInt64(bytes, 8);
        return (DateTime.FromFileTimeUtc(utcTime), id);
    }
}