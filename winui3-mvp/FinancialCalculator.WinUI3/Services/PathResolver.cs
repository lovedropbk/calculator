using System;
using System.IO;

namespace FinancialCalculator.WinUI3.Services
{
    internal static class PathResolver
    {
        // Resolve a file path under 'docs', walking up from the app base directory.
        // Falls back to baseDir if no docs root is found.
        internal static string GetDocsFilePath(params string[] pathParts)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var fileRelative = Path.Combine(pathParts ?? Array.Empty<string>());
            var docsRoot = LocateDocsRoot(baseDir);
            if (docsRoot != null)
            {
                return Path.Combine(docsRoot, fileRelative);
            }
            return Path.Combine(baseDir, fileRelative);
        }

        // Resolve the 'docs/parameters' directory. Falls back to baseDir/Parameters.
        internal static string GetParametersDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var docsRoot = LocateDocsRoot(baseDir);
            if (docsRoot != null)
            {
                var p = Path.Combine(docsRoot, "parameters");
                if (Directory.Exists(p)) return p;
            }
            return Path.Combine(baseDir, "Parameters");
        }

        private static string? LocateDocsRoot(string baseDir)
        {
            try
            {
                var direct = Path.Combine(baseDir, "docs");
                if (Directory.Exists(direct)) return direct;

                var current = new DirectoryInfo(baseDir);
                int depth = 10;
                while (current != null && depth-- > 0)
                {
                    var check = Path.Combine(current.FullName, "winui3-mvp", "docs");
                    if (Directory.Exists(check)) return check;

                    check = Path.Combine(current.FullName, "docs");
                    if (Directory.Exists(check)) return check;

                    current = current.Parent;
                }
            }
            catch
            {
                // best-effort
            }
            return null;
        }
    }
}