using tgv_benchmark;
using tgv_benchmark.Apps;

IApp? app = args.Contains("asp") ? new AspNetApp()
    : args.Contains("tgv") ? new TgvApp()
    : null;

if (app == null)
    throw new Exception("No app selected");

app.Run();
Console.WriteLine($"[{DateTime.Now:yyyy.MM.dd HH:mm:ss.fff}] Server started at: http://localhost:7000");
Console.WriteLine("Press any key to exit...");
Console.ReadKey(true);