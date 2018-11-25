using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text;
using Xunit;

namespace DevServer.Tests
{
    public class HttpServerResponseTests
    {
        [Fact]
        public void HtmlResponse_DefaultParams_IsSuccessfulHtmlResponse()
        {
            var response = HttpServerResponse.HtmlResponse("Hello");

            Assert.Equal("Hello", new StreamReader(response.Body).ReadToEnd());
            Assert.Equal("text/html", response.ContentType);
            Assert.Equal(200, (int)response.StatusCode);

            response.Body.Dispose();
        }

        [Fact]
        public void StringResponse_WithSpecifiedParameters_UsesArguments()
        {
            var response = HttpServerResponse.StringResponse("{}", "application/json", HttpStatusCode.OK);

            Assert.Equal("{}", new StreamReader(response.Body).ReadToEnd());
            Assert.Equal("application/json", response.ContentType);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response.Body.Dispose();
        }

        [Fact]
        public void FileResponse_WithModifiedFileTime_SetsAsLastModifiedTimeHeader()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\myserver\hello.json"] = new MockFileData("{}")
            });
            // set file modification time
            var written = DateTime.Now.AddDays(-1);
            fs.File.SetLastWriteTime(@"C:\myserver\hello.json", written);

            // system under test
            var response = HttpServerResponse.FileResponse(fs, @"C:\myserver\hello.json");

            Assert.Equal("{}", new StreamReader(response.Body).ReadToEnd());
            Assert.Equal(written.ToString("r"), response.Headers[HttpRequestHeader.LastModified]);

            response.Body.Dispose();
        }
    }
}
