using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Protocol.Core.Types;

namespace PackageHelper
{
    static class Helper
    {
        private const string RootMarker = "discover-packages.ps1";
        private static IReadOnlyList<string> ExtensionsToRemove = new List<string> { ".json", ".csv" };

        public static string GetGraphFileName(string graphType, string variantName, string solutionName)
        {
            ValidateGraphType(graphType);
            ValidateSolutionName(solutionName);
            ValidateVariantName(variantName);

            if (variantName != null)
            {
                return $"{graphType}-{variantName}-{solutionName}";
            }
            else
            {
                return $"{graphType}-{solutionName}";
            }
        }

        public static void ValidateGraphType(string graphType)
        {
            if (graphType == null)
            {
                throw new ArgumentNullException(nameof(graphType));
            }

            if (graphType.Contains('-'))
            {
                throw new ArgumentException("The graph type cannot contain hyphens.", nameof(graphType));
            }
        }

        public static void ValidateSolutionName(string solutionName)
        {
            if (solutionName == null)
            {
                throw new ArgumentNullException(nameof(solutionName));
            }

            if (solutionName.Contains('-'))
            {
                throw new ArgumentException("The solution name cannot contain hyphens.", nameof(solutionName));
            }
        }

        public static void ValidateVariantName(string variantName)
        {
            if (variantName != null && variantName.Contains('-'))
            {
                throw new ArgumentException("The variant name cannot contain hyphens.", nameof(variantName));
            }
        }

        public static bool TryParseFileName(string path, out string fileType, out string variantName, out string solutionName)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);

            foreach (var extension in ExtensionsToRemove)
            {
                if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - extension.Length);
                }
            }

            var pieces = fileName.Split('-');

            if (pieces.Length == 2)
            {
                fileType = pieces[0];
                variantName = null;
                solutionName = pieces[1];
                return true;
            }
            else if (pieces.Length >= 3)
            {
                fileType = pieces[0];
                variantName = pieces[1];
                solutionName = pieces[2];
                return true;
            }
            else
            {
                fileType = null;
                variantName = null;
                solutionName = null;
                return false;
            }
        }

        public static void Assert(bool assertion, string message)
        {
            if (!assertion)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static string GetLogTimestamp()
        {
            return DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssffff");
        }

        public static string GetExcelTimestamp(DateTimeOffset input)
        {
            return input.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static SourceCacheContext GetCacheContext()
        {
            return new SourceCacheContext
            {
                DirectDownload = true,
                NoCache = true,
            };
        }

        public static bool TryFindRoot(out string dir)
        {
            dir = Directory.GetCurrentDirectory();
            var tried = new List<string>();
            bool foundRoot;
            do
            {
                tried.Add(dir);
                foundRoot = Directory
                    .EnumerateFiles(dir)
                    .Select(filePath => Path.GetFileName(filePath))
                    .Contains(RootMarker);

                if (!foundRoot)
                {
                    dir = Path.GetDirectoryName(dir);
                }
            }
            while (dir != null && !foundRoot);

            if (!foundRoot)
            {
                Console.WriteLine($"Could not find {RootMarker} in the current or parent directories.");
                Console.WriteLine("Tried:");
                foreach (var path in tried)
                {
                    Console.WriteLine($"  {path}");
                }

                dir = null;
                return false;
            }

            return true;
        }
    }
}
