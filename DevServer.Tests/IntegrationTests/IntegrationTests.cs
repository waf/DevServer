using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DevServer.Tests
{
    /// <summary>
    /// Integration tests. These tests create and assert against a running webserver!
    /// </summary>
    public class IntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task ServeWebsite()
        {
            var http = new HttpClient();
            var cwd = Path.Combine(
                Directory.GetCurrentDirectory(),
                @"IntegrationTests\WebRoot\"
            );
            using(var server = new HttpServer(cwd, "localhost", 8080, enableAutorefresh: false))
            {
                server.Start(CancellationToken.None);

                await Get_NoPath_ShouldReturnIndexHtml(http);
                await Get_IndexPath_ShouldReturnIndexHtml(http);
                await Get_DirectoryPath_ShouldReturnDirectoryListing(http);
            } // server should end when it is disposed
        }

        private static async Task Get_NoPath_ShouldReturnIndexHtml(HttpClient http)
        {
            var response = await http.GetAsync("http://localhost:8080/");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", content);
        }

        private static async Task Get_IndexPath_ShouldReturnIndexHtml(HttpClient http)
        {
            var response = await http.GetAsync("http://localhost:8080/index.html");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World", content);
        }

        private static async Task Get_DirectoryPath_ShouldReturnDirectoryListing(HttpClient http)
        {
            var response = await http.GetAsync("http://localhost:8080/fruits/");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Directory Listing", content);
            Assert.Contains("apple", content);
            Assert.Contains("pineapple", content);
            Assert.Contains("tomato", content);
        }
    }
}
