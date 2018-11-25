using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.IO.Abstractions;

namespace DevServer
{
    /// <summary>
    /// Simple <see cref="HttpListener" />-based server for local development.
    /// </summary>
    /// <remarks>This class is disposable and should be disposed.</remarks>
    public sealed class HttpServer : IDisposable
    {
        private HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;

        private readonly RequestHandler requestHandler;
        private readonly string host;
        private readonly int port;
        private readonly AutoRefresh autoRefresher;
        private readonly bool isAutoRefreshEnabled;

        public HttpServer(string path, string host, int port, bool enableAutorefresh = false)
        {
            this.requestHandler = new RequestHandler(path, new FileSystem());
            this.host = host;
            this.port = port;
            this.isAutoRefreshEnabled = enableAutorefresh;

            if (isAutoRefreshEnabled)
            {
                this.autoRefresher = new AutoRefresh();
            }
        }

        /// <summary>
        /// Starts the HTTP server. The server will stop when the cancellation token is triggered
        /// or the HttpServer instance is disposed.
        /// </summary>
        public void Start(CancellationToken cancellationToken)
        {
            lock (requestHandler)
            {
                if (cancellationTokenSource != null) throw new InvalidOperationException("HttpServer already started.");
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            Task.Run(() => Listen(cancellationTokenSource.Token));

            if(isAutoRefreshEnabled)
            {
                Task.Run(() => autoRefresher.KeepAliveLoop(cancellationTokenSource.Token));
            }
        }

        private async Task Listen(CancellationToken token)
        {
            // start listener
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://{0}:{1}/", host, port));
                listener.Start();
            }
            catch (Exception e)
            {
                ReportError(e);
                return;
            }

            // wait for requests
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(async () =>
                    {
                        try { await ProcessContext(context, token).ConfigureAwait(false); }
                        catch (Exception e) { ReportError(e); }
                    });
                }
                catch (Exception e)
                {
                    ReportError(e);
                    return;
                }
            }
        }

        /// <summary>
        /// Tell all connected clients to refresh the page
        /// </summary>
        public void SendClientRefresh() =>
            autoRefresher?.SendClientRefresh();

        /// <summary>
        /// Process the received context from the <see cref="HttpListener"/>.
        /// The context represents a single request/response.
        /// </summary>
        private async Task ProcessContext(HttpListenerContext context, CancellationToken token)
        {
            HttpServerResponse response;
            try
            {
                response = GenerateResponse(context);
            }
            catch (Exception e)
            {
                ReportError(e);
                response = HttpServerResponse.HtmlResponse("ERROR: " + e.Message, HttpStatusCode.InternalServerError);
            }

            if(!token.IsCancellationRequested)
            {
                await StreamResponseAsync(context, response, token).ConfigureAwait(false);
            }
        }

        private HttpServerResponse GenerateResponse(HttpListenerContext context)
        {
            string url = context.Request.Url.AbsolutePath;

            if (isAutoRefreshEnabled && url == AutoRefresh.ServerEndpoint)
            {
                return autoRefresher.SendPollResponse();
                // don't log refresh polling to the console on purpose, because it's too noisy.
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - HTTP {context.Request.HttpMethod} {url}");
            return requestHandler.GenerateResponse(url);
        }

        private async Task StreamResponseAsync(HttpListenerContext context, HttpServerResponse response, CancellationToken token)
        {
            var outputStream = context.Response.OutputStream;

            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.ContentType + "; charset=utf-8";
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            using (response.Body)
            {
                await response.Body.CopyToAsync(outputStream, 81920, token).ConfigureAwait(false);
                if(isAutoRefreshEnabled)
                {
                    await autoRefresher.AppendAutoRefreshJavaScript(response, outputStream, token).ConfigureAwait(false);
                }
                await outputStream.FlushAsync(token).ConfigureAwait(false);
            }
            outputStream.Close();
        }

        private static void ReportError(Exception e, [CallerMemberName] string caller = null) =>
            Console.Error.WriteLine($"ERROR ({caller}): {e.Message}");

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            if(listener == null)
            {
                return;
            }
            if (listener.IsListening)
            {
                listener.Stop();
            }
            listener.Close();
            listener = null;
        }
    }
}