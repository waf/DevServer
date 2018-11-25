# Dev Server

A very simple development server built on top of HttpListener. Intended to be embedded in command line applications.

- Works in CoreRT with AOT compilation (no reflection). This is the [main benefit over embedding Kestrel](https://github.com/aspnet/Home/issues/3079).
- Supports serving local files, default index.html, and directory listings 
- Supports a simple, optional "live-reload" workflow
- targets dotnet standard
- Fully async
- About 350 lines of unit-tested, commented code.

# Usage

```csharp
using(var server = new HttpServer("C:\path\to\webroot\", "localhost", 8080, enableAutorefresh: false))
{
    server.Start(CancellationToken.None); // or a cancellation token, if you require cancellation
    Console.ReadKey("server started");
} // server will end when it is disposed or the cancellation token is triggered
```

If you're using the autorefresh (i.e. "live reload"), call `server.SendClientRefresh()`
to trigger a browser refresh. This is most often called in an event handler, like `FileSystemWatcher.Changed`.

# Installing

Right now there's no nuget package; if you want one, open a GitHub issue and I'll create a package!

# Development

- HttpServer: the main user entry point. Its responsibility is managing the .NET HttpListener API
- RequestHandler: maps an http request to an http response
- HttpServerResponse: a model of an http response, contains some static functions to create common response types, like static files or 404 errors.