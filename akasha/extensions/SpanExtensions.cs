using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace akasha.extensions;

public static class SpanExtensions
{
    public static readonly ReadOnlyMemory<byte> HttpNewLineBreak = "\r\n\r\n"u8.ToArray();

    private static readonly FieldInfo f_List_items =
        typeof(List<byte>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Convert to <see cref="Span{T}"/><br/><br/>
    /// <b>WARNING!</b><br/>
    /// Utilize reflection, use with wisdom!
    /// </summary>
    /// <param name="bytes"></param>
    public static Span<byte> AsSpan(this List<byte> bytes)
    {
        return (f_List_items.GetValue(bytes) as byte[]).AsSpan(0, bytes.Count);
    }

    public static void AddRange<T>(this List<T> source, Span<T> span)
    {
        if (span.IsEmpty) return;

        // resize list only once
        if (source.Capacity < span.Length + source.Count)
        {
            source.Capacity += span.Length;
        }

        var e = span.GetEnumerator();
        while (e.MoveNext()) source.Add(e.Current);
    }

    public static string ToUtf8String(this Span<byte> bytes) => Encoding.UTF8.GetString(bytes.ToArray());

    public static Span<T> Concat<T>(this T[] left, Span<T> right)
    {
        if (right.Length == 0) return left;
        if (left.Length == 0) return right;

        var buff = new T[left.Length + right.Length];
        
        var resultSpan = buff.AsSpan();
        left.CopyTo(resultSpan);
        
        var from = left.Length;
        right.CopyTo(resultSpan.Slice(from));

        return resultSpan;
    }

    public static int FindIndex<T>(this Span<T> source, ReadOnlySpan<T> find, int startIndex = 0)
        where T : IEquatable<T>
    {
        var index = source.Slice(startIndex).IndexOf(find);
        if (index < 0) return -1;
        
        return startIndex + index;
    }
}