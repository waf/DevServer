using System.IO.Abstractions;

namespace DevServer
{
    static class PathUtils
    {
        /// <summary>
        /// Get rid of windows directory separators. `/` works cross platform, even windows.
        /// </summary>
        public static string NormalizeDirectorySeparators(string path) =>
            path.Replace('\\', '/');

        /// <summary>
        /// Combine filepaths, returning the full path of the resulting location.
        /// </summary>
        /// <example>
        /// ["C:\", "Users", "Joe", ".."]  would combine to "C:\Users"
        /// </example>
        public static string CombineToFullPath(IFileSystem fs, params string[] paths)
        {
            string combined = fs.Path.Combine(paths);
            string fullPath = fs.Path.GetFullPath(combined); // collapses foo/../bar.txt to /bar.txt
            return NormalizeDirectorySeparators(fullPath);
        }
    }
}
