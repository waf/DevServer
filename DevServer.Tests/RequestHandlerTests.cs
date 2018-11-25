using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using Xunit;

namespace DevServer.Tests
{
    public class RequestHandlerTests
    {
        private RequestHandler requestHandler;

        public RequestHandlerTests()
        {
            var fs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [@"C:\myserver\index.html"] = new MockFileData("Welcome to my blog"),
                [@"C:\myserver\puppies.html"] = new MockFileData("this a page about my puppies"),
                [@"C:\myserver\puppies\fido.jpg"] = new MockFileData(Array.Empty<byte>()),
                [@"C:\myserver\puppies\boxer.jpg"] = new MockFileData(Array.Empty<byte>()),
                [@"C:\myserver\puppies\pogo.jpg"] = new MockFileData(Array.Empty<byte>()),
                [@"C:\private.txt"] = new MockFileData("the password is passw0rd"),
            });
            this.requestHandler = new RequestHandler(@"C:\myserver\", fs);
        }

        [Fact]
        public void GenerateResponse_WithAbsolutePath_ReturnsContent()
        {
            var response = requestHandler.GenerateResponse("/index.html");
            Assert.Equal("Welcome to my blog", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void GenerateResponse_WithRelativePath_ReturnsContent()
        {
            var response = requestHandler.GenerateResponse("index.html");
            Assert.Equal("Welcome to my blog", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void GenerateResponse_WithBoundedDirectoryTraversal_EvaluatesTraversal()
        {
            // this traversal is ok, because it's still under the web root
            var response = requestHandler.GenerateResponse("/puppies/../index.html");
            Assert.Equal("Welcome to my blog", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void GenerateResponse_WithEscapingDirectoryTraversal_DoesNotEvaluatesTraversal()
        {
            // this traversal is NOT ok, because it's trying to escape the web root.
            var response = requestHandler.GenerateResponse("/../private.txt");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public void GenerateResponse_WithHtmlFile_ReturnsHtmlContentType()
        {
            var response = requestHandler.GenerateResponse("/index.html");
            Assert.Equal("text/html", response.ContentType);
        }

        [Fact]
        public void GenerateResponse_WithJpegFile_ReturnsJpegContentType()
        {
            var response = requestHandler.GenerateResponse("/puppies/fido.jpg");
            Assert.Equal("image/jpeg", response.ContentType);
        }

        [Fact]
        public void GenerateResponse_NoFileName_ReturnsIndexHtml()
        {
            var response = requestHandler.GenerateResponse("/");
            Assert.Equal("Welcome to my blog", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public void GenerateResponse_Directory_ReturnsDirectoryListing()
        {
            var response = requestHandler.GenerateResponse("/puppies/");
            string content = new StreamReader(response.Body).ReadToEnd();

            Assert.Contains("Directory Listing", content);
            // we should assert that the listing has the three jpg files here, but this bug prevents it:
            // https://github.com/System-IO-Abstractions/System.IO.Abstractions/issues/405
        }
    }
}
