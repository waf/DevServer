using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DevServer
{
    /// <summary>
    /// Auto-Refresh for browsers, using the Server Sent Events API
    /// </summary>
    public sealed class AutoRefresh
    {
        public const string ServerEndpoint = "/dev-server-auto-refresh";

        private readonly byte[] javascript;
        private readonly StringBuilder serverSentEvents = new StringBuilder();

        public AutoRefresh()
        {
            this.javascript = Encoding.UTF8.GetBytes($@"
                <script>
                    new EventSource('{ServerEndpoint}').onmessage = function(e) {{
                        window.location.reload();
                    }}
                </script>
            ");
        }

        public async Task KeepAliveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), token).ConfigureAwait(false);
                this.SendKeepAliveNotification();
            }
        }

        public HttpServerResponse SendPollResponse()
        {
            var response = serverSentEvents.ToString();
            serverSentEvents.Clear();
            return HttpServerResponse.StringResponse(response, "text/event-stream");
        }

        public void SendClientRefresh() =>
            serverSentEvents
                .AppendLine("data: refresh")
                .AppendLine();

        private void SendKeepAliveNotification() =>
            serverSentEvents
                .AppendLine(":stayin' alive") // ':' is the comment character for SSE
                .AppendLine();

        public async Task AppendAutoRefreshJavaScript(HttpServerResponse response, Stream outputStream, CancellationToken token)
        {
            if (response.ContentType == "text/html")
            {
                await outputStream.WriteAsync(javascript, 0, javascript.Length, token).ConfigureAwait(false);
            }
        }
    }
}
