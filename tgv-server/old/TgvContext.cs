// using System;
// using System.Collections.Specialized;
// using System.IO;
// using System.Net;
// using System.Net.Http;
// using System.Net.Sockets;
// using System.Reflection;
// using System.Text;
// using System.Threading.Tasks;
// using tgv_core.api;
// using tgv_core.imp;
//
// namespace tgv_server;
//
// public class TgvContext : Context
// {
//     private static readonly string ServerHeader =
//         $"TgvServer/${Assembly.GetAssembly(typeof(TgvContext)).GetName().Version}";
//
//     private readonly NetworkStream _stream;
//     private readonly Version _protocol;
//     private readonly string _bodyRaw;
//     private readonly Settings _settings;
//
//     private bool _sentStarted;
//
//     public TgvContext(NetworkStream stream, Guid traceId, Logger logger, Settings settings,
//         HttpMethod method, Version protocol, Uri uri, NameValueCollection query, NameValueCollection headers,
//         string bodyRaw)
//         : base(method, traceId, uri, logger, headers, query)
//     {
//         _stream = stream;
//         _protocol = protocol;
//         _bodyRaw = bodyRaw;
//         _settings = settings;
//
//
//         if (settings.AddServerHeader)
//             ResponseHeaders[HttpResponseHeader.Server.ToString()] = ServerHeader;
//     }
//
//     public override bool WasSent => _sentStarted;
//
//     public override Task<string> Body() => Task.FromResult(_bodyRaw);
//
//     public override Task Redirect(string path, HttpStatusCode code = HttpStatusCode.Moved)
//     {
//         ResponseHeaders[HttpResponseHeader.Location.ToString()] = path;
//         return SendRaw((byte[])null, code, "plain/text");
//     }
//
//     private async Task SendHttpResponse(HttpStatusCode code, string? body)
//     {
//         _sentStarted = true;
//         
//         if (body != null)
//             ResponseHeaders[HttpRequestHeader.ContentLength.ToString()] = body.Length.ToString();
//
//         var sb = new StringBuilder();
//         sb.AppendLine($"HTTP/{HttpVersion.Version11} {(int)code} {code.ToString()}");
//         foreach (var key in ResponseHeaders.AllKeys)
//             sb.AppendLine($"{key}: {ResponseHeaders[key]}");
//         
//         sb.AppendLine();
//         if (!string.IsNullOrEmpty(body))
//         {
//             sb.Append(body ?? string.Empty);
//             sb.AppendLine();
//         }
//         sb.AppendLine();
//         
//         var bytes = Encoding.UTF8.GetBytes(sb.ToString());
//         await _stream.WriteAsync(bytes, 0, bytes.Length);
//         await _stream.FlushAsync();
//     }
//
//     protected override Task SendRaw(byte[]? bytes, HttpStatusCode code, string? contentType)
//     {
//         if (!string.IsNullOrEmpty(contentType))
//             ResponseHeaders[HttpResponseHeader.ContentType.ToString()] = contentType;
//
//         var body = bytes == null ? null : Encoding.UTF8.GetString(bytes);
//         return SendHttpResponse(code, body);
//     }
//
//     protected override async Task SendRaw(Stream stream, HttpStatusCode code, string contentType)
//     {
//         if (!string.IsNullOrEmpty(contentType))
//             ResponseHeaders[HttpResponseHeader.ContentType.ToString()] = contentType;
//         
//         using var reader = new StreamReader(stream, Encoding.UTF8);
//         var body = await reader.ReadToEndAsync();
//         await SendHttpResponse(code, body);
//     }
// }