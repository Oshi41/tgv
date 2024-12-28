using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace akasha.extensions;

public static class SpanExtensions
{
    public static readonly ReadOnlyMemory<byte> HttpNewLineBreak = "\r\n\r\n"u8.ToArray();

    private static readonly FieldInfo f_List_items =
        typeof(List<byte>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo f_Queue_array = typeof(Queue<byte>)
        .GetField("_array", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo f_Queue_head = typeof(Queue<byte>)
        .GetField("_head", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo f_Queue_tail = typeof(Queue<byte>)
        .GetField("_tail", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <inheritdoc cref="HttpNewLineIndex(System.Span{byte})"/>
    public static int HttpNewLineIndex(this ReadOnlySpan<byte> bytes) => bytes.IndexOf(HttpNewLineBreak.Span);

    /// <summary>
    /// Searches last index of HTTP new line break (CR-LF-CR-LF). 
    /// </summary>
    /// <param name="bytes">Bytes data span</param>
    /// <returns>-1 if line break was not found, zero-based index otherwise</returns>
    public static int HttpNewLineIndex(this Span<byte> bytes) => bytes.IndexOf(HttpNewLineBreak.Span);

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

    /// <summary>
    /// Convert to <see cref="Span{T}"/><br/><br/>
    /// <b>WARNING!</b><br/>
    /// Utilize reflection, use with wisdom!
    /// </summary>
    /// <remarks>
    /// <see cref="Queue{T}"/> is not a really great way to retreive <see cref="Span{T}"/>. It
    /// </remarks>
    /// <param name="bytes">Bytes data</param>
    public static Span<byte> AsSpan(this Queue<byte> bytes)
    {
        var array = (f_Queue_array.GetValue(bytes) as byte[]);
        var head = (int)(f_Queue_head.GetValue(bytes));
        var tail = (int)(f_Queue_tail.GetValue(bytes));

        // easy condition
        if (head < tail)
        {
            return array.AsSpan(head, bytes.Count);
        }

        return bytes.AsEnumerable().AsSpanDangerous();
    }

    /// <summary>
    /// Convert to <see cref="Span{T}"/><br/>
    /// </summary>
    /// <remarks>Creates new array which may be very expensive</remarks>
    /// <param name="bytes">Bytes data</param>
    public static Span<byte> AsSpanDangerous(this IEnumerable<byte> bytes) => bytes.ToArray().AsSpan();

    public static bool Split<T>(this Span<T> bytes, ReadOnlySpan<T> splitter, out Span<T> head, out Span<T> tail)
        where T : IEquatable<T>
    {
        head = bytes;
        tail = Span<T>.Empty;

        if (bytes.IsEmpty || splitter.IsEmpty) return false;

        var index = bytes.IndexOf(splitter);
        if (index < 0) return false;

        // found the end of the line
        head = bytes.Slice(0, index);

        index += splitter.Length + 1; // need to skip the splitter fully
        if (index < bytes.Length)
            tail = bytes.Slice(index);

        return true;
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
}