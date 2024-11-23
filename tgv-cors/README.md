# TGV-Cors

TGV-Cors - middleware for TGV HTTP server. 
It enables cross-origin resource sharing (CORS) for your application, making it accessible from different origins.

_TGV is a fast, simple, and intuitive HTTP server library for C#. 
Inspired by ExpressJS, TGV is designed to make building 
web applications straightforward, even for developers with minimal experience_

---

## **Usage**
Available via [NuGet](https://www.nuget.org/packages/tgv-cors/)

```
using tgv_cors;
using tgv;

var app = new App();
app.Use(Cors(settings));
```

## Configuration

### Settings

| **Setting**           | **Description**                                                                 | **Default Value**                          |
|------------------------|---------------------------------------------------------------------------------|--------------------------------------------|
| **Allowed Origins**    | Configure which origins can access your server. Options:                        | All origins  (`*`)                         |
|                        | - `string[] origins`: Specify exact matches.                                    |                                            |
|                        | - `regex[] origins`: Use regular expressions for matching.                      |                                            |
|                        | - `Task<bool> (Context ctx)`: Custom validation callback.                       |                                            |
| **Allowed Methods**    | Specify allowed HTTP methods for CORS requests.                                 | `GET, DELETE, POST, PATCH, PUT, HEAD`      |
| **Allowed Headers**    | Specify allowed custom headers for CORS requests (`Access-Control-Allow-Headers`).| Empty array, ignore header               |
| **Exposed Headers**    | Specify headers exposed to the client (`Access-Control-Expose-Headers`).         | Empty array, ignore header                |
| **Use Credentials**    | Allow credentials (cookies, HTTP authentication) to be sent.                    | `false`                                    |
| **Max Age**            | Define how long the results of a preflight request can be cached (`Access-Control-Max-Age`).| `null` (no cache)              |
| **CORS Response Code** | The HTTP status code for CORS responses.                                        | `204, No content`                          |
| **Continue Preflight** | Whether to continue routing after handling preflight requests.                  | `false`                                    |


## Usage Examples

### **Basic CORS Setup**
Allow all origins with default settings:
```
var app = new App();
app.Use(Cors(new CorsSettings()));
```

### **Restrict to Specific Origins**
Allow requests only from specific origins:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    Origins = new[] { "https://example.com", "https://api.example.com" }
}));
```

### **Using Regex for Origins**
Allow requests from origins matching a pattern:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    Origins = new[] { new Regex(".+example.+\.com") }
}));
```

### **Custom Origin Validation**
Use a callback to allow or deny origins dynamically:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    Origins = ctx => Task.FromResult(ctx.Request.Headers["Origin"] == "https://trusted.com")
}));
```

### **Allow Specific Methods**
Restrict allowed HTTP methods:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    Methods = new[] { HttpMethod.Get, HttpMethod.Post }
}));
```

### **Exposing Headers**
Expose custom headers to clients:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    ExposedHeaders = new[] { "X-Custom-Header" }
}));
```

### **Allowing Credentials**
Enable credentials support:
```
var app = new App();
app.Use(Cors(new CorsSettings
{
    UseCredentials = true
}));
```