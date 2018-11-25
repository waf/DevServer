using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DevServer.Tests
{
    public class AutoRefreshTests
    {
        private readonly AutoRefresh autorefresh;

        public AutoRefreshTests()
        {
            this.autorefresh = new AutoRefresh();
        }

        [Fact]
        public async Task KeepAliveLoop_CancelRequested_IsCanceled()
        {
            var tokenSource = new CancellationTokenSource();

            // system under test
            var task = autorefresh.KeepAliveLoop(tokenSource.Token);
            tokenSource.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        }

        [Fact]
        public void SendPollResponse_WithTwoRefreshes_ContainsTwoRefreshes()
        {
            autorefresh.SendClientRefresh();
            autorefresh.SendClientRefresh();

            var response = autorefresh.SendPollResponse();

            Assert.Equal(
                "data: refresh\r\n\r\ndata: refresh\r\n\r\n",
                new StreamReader(response.Body).ReadToEnd()
            );
        }

        [Fact]
        public async Task AppendAutoRefreshJavaScript_HtmlFile_AppendsAutoRefreshJavaScript()
        {
            var response = CreateMemoryStreamResponse("howdy", "text/html");

            // system under test
            await autorefresh.AppendAutoRefreshJavaScript(response, response.Body, CancellationToken.None);

            response.Body.Position = 0;
            var content = new StreamReader(response.Body).ReadToEnd();

            Assert.Contains("howdy", content);
            Assert.Contains("window.location.reload", content);

            response.Body.Dispose();
        }

        [Fact]
        public async Task AppendAutoRefreshJavaScript_NotHtmlFile_DoesNotAppendAutoRefreshJavaScript()
        {
            var response = CreateMemoryStreamResponse("{}", "application/json");

            // system under test
            await autorefresh.AppendAutoRefreshJavaScript(response, response.Body, CancellationToken.None);

            response.Body.Position = 0;
            var content = new StreamReader(response.Body).ReadToEnd();

            Assert.Equal("{}", content.Trim());

            response.Body.Dispose();
        }

        private static HttpServerResponse CreateMemoryStreamResponse(string content, string contentType)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine(content);
            writer.Flush();
            var response = new HttpServerResponse(HttpStatusCode.OK, new WebHeaderCollection(), stream, contentType);
            return response;
        }
    }
}
