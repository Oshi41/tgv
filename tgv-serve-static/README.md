# TGV-serve-static

This middleware component is designed to handle the serving of static files,
such as HTML, CSS, JavaScript, images, etc., making it easy to serve these
assets directly from the server with minimal configuration.

Available on [NuGet](https://www.nuget.org/packages/tgv-serve-static/).

_TGV is a fast, simple, and intuitive HTTP server library for C#. Inspired by ExpressJS, 
TGV is designed to make building web applications straightforward, 
even for developers with minimal experience._ 

---

## **Overview**

Static Files Middleware allows an application to serve files directly from the
file system. This is suitable for web applications that need to serve static
content without routing to specific controllers or actions.
The middleware listens for HTTP GET and HEAD requests and processes file
serving operations.

## **Features**

- **Serving Static Files:** The middleware can serve static files from a specified directory.
- **File Caching:** Employs a simple caching mechanism to store file contents in memory, reducing file I/O operations for frequently accessed files.
- **File Watching:** Utilizes `FileSystemWatcher` to keep track of changes in the file system, automatically updating cached content when files are modified, renamed, or deleted.
- **Method Handling:** Responds with the correct HTTP methods (GET and HEAD) and can handle method not allowed and fall-through scenarios.

## **Usage**
```
public class StaticFilesConfig
{
    public string SourceDirectory { get; set; }
    public bool FallThrough { get; set; } // Determine if unhandled requests fall through to the next middleware
}

var app = new App();
var config = new StaticFilesConfig
{
    SourceDirectory = "wwwroot",
    FallThrough = true // or false based on your need
};

app.ServeStatic(config);
```