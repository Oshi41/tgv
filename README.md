![tgv_small](https://github.com/user-attachments/assets/854e13a7-90c1-4e95-b179-b794841626ad)

## Fast, minimalistic and cross-platform HTTP server

```
using tgv;

var app = new App();
app.Get("/", async (ctx, next, exception) => {
  await ctx.Text("Hello world!");
});

app.Start(7000); 
```

## Motivation

TGV aims to bring the simplicity and intuitiveness of frameworks like [ExpressJS](https://github.com/expressjs/express) to the .NET and C# ecosystem.
Unlike the more complex and sometimes overwhelming structure of [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet), TGV offers a straightforward approach to building web applications.


## Installation

Available as [Nugget package](https://www.nuget.org/packages/tgv).

Project written with [.Net Standart 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-1-0).

Installation is done using `dotnet add package tgv1`


## Features
HTTP \ HTTPS support

Routing system similar to Express server.

HTTP helper function (redirecting, sending files, JSON, etc.)

Cross-platform (special thanks for [Watson.Lite](https://github.com/dotnet/WatsonWebserver) http server implementation)
