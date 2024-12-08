# TGV-Server

The TGV Server is a robust and efficient server
component built on top of the NetCoreServer framework.
It is designed to handle HTTP and HTTPS requests
efficiently, providing a flexible setup for various
network service architectures.

_TGV is a fast, simple, and intuitive HTTP server library for C#.
Inspired by ExpressJS, TGV is designed to make building web applications straightforward,
even for developers with minimal experience._

## Overview
TGV Server provides core functionality to support
network services that require handling HTTP/HTTPS
protocols. It is built to leverage asynchronous
I/O operations to enhance performance and scalability.
The server includes features to handle requests,
manage server state, log activities, and more.

## Usage

```
var settings = new TgvSettings
{
    AddServerHeader = true, // Optional, defaults to true
    Certificate = new X509Certificate2("path/to/certificate.pfx", "password")
};
var logger = new Logger();

var app = new App(handler => new TgvServer(settings, handler, logger));
app.StartAsync(3001);
```