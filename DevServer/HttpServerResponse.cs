using System.Net;
using MimeTypes;
using System.Text;
using System.IO;
using System.IO.Abstractions;

namespace DevServer
{
    /// <summary>
    /// Represents an HTTP response, with static factory methods
    /// for producing different types of responses.
    /// </summary>
    public sealed class HttpServerResponse
    {
        public HttpServerResponse(HttpStatusCode statusCode, WebHeaderCollection headers, Stream body, string contentType)
        {
            StatusCode = statusCode;
            Headers = headers;
            Body = body;
            ContentType = contentType;
        }

        public HttpStatusCode StatusCode { get; }
        public WebHeaderCollection Headers { get; }
        public Stream Body { get; }
        public string ContentType { get; }

        /// <summary>
        /// Create an HTTP response that contains the specified file.
        /// </summary>
        public static HttpServerResponse FileResponse(IFileSystem fileSystem, string filePath)
        {
            var headers = new WebHeaderCollection()
            {
                { "Last-Modified", fileSystem.File.GetLastWriteTime(filePath).ToString("r") }
            };
            string contentType = MimeTypeMap.GetMimeType(fileSystem.Path.GetExtension(filePath));
            Stream body = fileSystem.FileStream.Create(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

            return new HttpServerResponse(HttpStatusCode.OK, headers, body, contentType);
        }

        /// <summary>
        /// Create an HTTP response with an html body. Convenience method over StringResponse.
        /// </summary>
        public static HttpServerResponse HtmlResponse(string html, HttpStatusCode status = HttpStatusCode.OK) =>
            StringResponse(html, "text/html", status);

        /// <summary>
        /// Create an HTTP response with a string body.
        /// </summary>
        public static HttpServerResponse StringResponse(string stringResponse, string mimeType, HttpStatusCode status = HttpStatusCode.OK)
        {
            return new HttpServerResponse(
                status,
                new WebHeaderCollection(),
                new MemoryStream(Encoding.UTF8.GetBytes(stringResponse ?? "")),
                mimeType
            );
        }
    }
}