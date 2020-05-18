using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PackageHelper
{
    public static class Helper
    {
        private const string RootMarker = "discover-packages.ps1";

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
