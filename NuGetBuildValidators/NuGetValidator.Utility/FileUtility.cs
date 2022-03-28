using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGetValidator.Utility
{
    public class FileUtility
    {
        public static string[] GetDlls(string root, bool isArtifacts = false, string filterPathsContaining = null)
        {
            if (isArtifacts)
            {
                var files = new List<string>();
                var directories = Directory.GetDirectories(root)
                    .Where(d => !Path.GetFileName(d).Equals("NuGet.VisualStudio.Client") && // Skip NuGet.VisualStudio.
                                (Path.GetFileName(d).StartsWith("NuGet", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(d).StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
                                );

                foreach (var dir in directories)
                {
                    var expectedDllName = Path.GetFileName(dir) + ".dll";
                    if (Path.GetFileName(dir).Equals("Microsoft.Web.Xdt.2.1.1"))
                    {
                        expectedDllName = "Microsoft.Web.XmlTransform.dll";
                    }

                    var englishDlls = Directory.GetFiles(dir, expectedDllName, SearchOption.AllDirectories)
                        .Where(p => p.Contains("bin") || (Path.GetFileName(dir).StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) && p.Contains("lib")))
                        .Where(p => !p.Contains("ilmerge"))
                        .Where(p => filterPathsContaining == null || !p.Contains(filterPathsContaining))
                        .OrderBy(p => p);

                    if (englishDlls.Any())
                    {
                        files.Add(englishDlls.First());
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: No dll matching the directory name was found in {dir}");
                    }
                }

                return files.ToArray();
            }
            else
            {
                return Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly)
                    .Where(ShouldIncludeDll)
                    .ToArray();
            }
        }

        public static bool ShouldIncludeDll(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            if (fileName.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fileName.StartsWith("NuGet", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Split on chars and trim. Null or empty inputs will return an empty list.
        /// </summary>
        public static IList<string> Split(string s, params char[] chars)
        {
            if (!string.IsNullOrEmpty(s))
            {
                // Split on chars and trim all entries
                // After trimming remove any entries that are now empty due to trim.
                return s.Trim()
                    .Split(chars)
                    .Select(entry => entry.Trim())
                    .Where(entry => entry.Length != 0)
                    .ToList();
            }
            else
            {
                return Enumerable.Empty<string>()
                    .ToList();
            }
        }

        public static IList<string> ReadFilesFromFile(string file)
        {
            var fileList = Enumerable.Empty<string>()
                .ToList();

            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                using (StreamReader sr = File.OpenText(file))
                {
                    var s = String.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        fileList.Add(s);
                    }
                }
            }

            return fileList;
        }
    }
}
