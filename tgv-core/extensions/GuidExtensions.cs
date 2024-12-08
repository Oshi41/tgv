using System;

namespace tgv_core.extensions;

public static class GuidExtensions
{
    /// <summary>
    /// Creates a new Guid by embedding a specified long identifier and an optional DateTime value.
    /// </summary>
    /// <param name="id">The long identifier to include in the Guid.</param>
    /// <param name="dateTime">
    /// An optional DateTime value to include in the Guid. If not specified, the current DateTime is used.
    /// </param>
    /// <returns>A new Guid containing the long identifier and the DateTime value.</returns>
    public static Guid CreateId(this long id, DateTime? dateTime = null)
    {
        dateTime ??= DateTime.Now;
        var guidData = new byte[16];
        Array.Copy(BitConverter.GetBytes(dateTime.Value.ToFileTimeUtc()), guidData, 8);
        Array.Copy(BitConverter.GetBytes(id), 0, guidData, 8, 8);
        return new Guid(guidData);
    }

    /// <summary>
    /// Extracts a long identifier and a DateTime value from the specified Guid.
    /// </summary>
    /// <param name="guid">The Guid to extract the data from.</param>
    /// <returns>A tuple containing the extracted DateTime and long identifier.</returns>
    public static (DateTime dateTime, long id) FromId(this Guid guid)
    {
        var bytes = guid.ToByteArray();
        var utcTime = BitConverter.ToInt64(bytes, 0);
        var id = BitConverter.ToInt64(bytes, 8);
        return (DateTime.FromFileTimeUtc(utcTime), id);
    }
}