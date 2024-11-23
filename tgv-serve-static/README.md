# TGV-serve-static

The static file serving middleware is a part of TGV ecosystem. 
Middleware enables your application to serve static files
 (like `.html`, `.css`, `.js`, or log files) from a specified directory. 
It ensures secure access to files only within the given folder and its subfolders. 

Available on [NuGet](https://www.nuget.org/packages/tgv-serve-static/).

_TGV is a fast, simple, and intuitive HTTP server library for C#. Inspired by ExpressJS, 
TGV is designed to make building web applications straightforward, 
even for developers with minimal experience._ 

---

## **Usage**

```
using tgv_serve_static;
using tgv;

var app = new App();
app.ServeStatic(new StaticFilesConfig("C:/server/www/static"));
```

## Serving Default File
If a URL doesn't specify a file, the middleware serves index.html:

```
http://localhost:3000/ -> C:/server/www/static/index.html
http://localhost:3000/info.html -> C:/server/www/static/info.html
```
