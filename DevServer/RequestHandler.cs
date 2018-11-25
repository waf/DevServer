using System;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DevServer.Tests")]
namespace DevServer
{
    internal class RequestHandler
    {
        private const string IndexFile = "index.html";
        private readonly string rootPath;
        private readonly IFileSystem fs;

        public RequestHandler(string path, IFileSystem fs)
        {
            this.rootPath = PathUtils.NormalizeDirectorySeparators(path);
            this.fs = fs;
        }

        /// <summary>
        /// The core Request -> Response function
        /// </summary>
        public HttpServerResponse GenerateResponse(string requestPath)
        {
            string filename = WebUtility.UrlDecode(requestPath.TrimStart('/'));
            string filepath = PathUtils.CombineToFullPath(fs, rootPath, filename);

            if (!filepath.StartsWith(rootPath))
            {
                // directory traversal attempt, deny
                return HttpServerResponse.HtmlResponse("Not Found", HttpStatusCode.NotFound);
            }

            // serve a file
            if (fs.File.Exists(filepath))
            {
                return HttpServerResponse.FileResponse(fs, filepath);
            }

            // serve index.html
            string defaultIndexFile = fs.Path.Combine(filepath, IndexFile);
            if (fs.File.Exists(defaultIndexFile))
            {
                return HttpServerResponse.FileResponse(fs, defaultIndexFile);
            }

            // serve directory listing
            if (fs.Directory.Exists(filepath))
            {
                var listing = GenerateDirectoryListing(filepath);
                return HttpServerResponse.HtmlResponse(listing);
            }

            // 404
            return HttpServerResponse.HtmlResponse("Not Found", HttpStatusCode.NotFound);
        }

        private string GenerateDirectoryListing(string directory)
        {
            var entries = fs.Directory
                .GetFileSystemEntries(directory)
                .Select(file =>
                {
                    string relativePath = PathUtils.NormalizeDirectorySeparators(file).Replace(rootPath, "");
                    string display = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    return $"<li><a href='{relativePath}'>{display}</a></li>";
                });
            return $"{DirectoryListingHeader}<ul>{String.Concat(entries)}</ul>";
        }

        // hard-coded css is the best css.
        private const string DirectoryListingHeader = @"
            <style>
            body { font-family: sans-serif; }
            h1 { font-size: 16pt; }
            ul { padding: 0; }
            li {
              font-size: 14pt;
              list-style-type: none;
              padding: 5px 20px;
            }
            li:nth-child(odd) { background-color: #f8f8f8; }
            a { color: #0086c1; }
            </style>
            <h1>Directory Listing</h1>
        ";
    }
}
