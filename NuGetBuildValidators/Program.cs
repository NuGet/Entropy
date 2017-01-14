using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace NuGetStringChecker
{
    class Program
    {
        private static ConcurrentQueue<string> _errors = new ConcurrentQueue<string>();
        private static int _numberOfThreads = 8;

        public static void Main(string[] args)
        {
            if(args.Count() < 3)
            {
                Console.WriteLine("Please enter 3 arguments - ");
                Console.WriteLine("arg[0]: NuGet.Tools.Vsix path");
                Console.WriteLine("arg[1]: Path to extract NuGet.Tools.Vsix into. Folder need not be already present, but Program should have write access to the location.");
                Console.WriteLine("arg[2]: Path to the log File for writing errors. File need not be already present, but Program should have write access to the location.");
                Console.WriteLine("Exiting...");
                return;
            }

            var vsixPath = args[0]; //@"\\wsr-tc\Drops\NuGet.Signed.AllLanguages\latest-successful\Signed\VSIX\15\NuGet.Tools.vsix";
            var extractedVsixPath = args[1]; //@"F:\validation\NuGet.Tools";
            var logPath = args[2]; // @"F:\validation\log.txt";

            CleanExtractedFiles(extractedVsixPath);

            ExtractVsix(vsixPath, extractedVsixPath);

            var englishDlls = GetEnglishDlls(extractedVsixPath);

            ParallelOptions ops = new ParallelOptions { MaxDegreeOfParallelism = _numberOfThreads };
            Parallel.ForEach(englishDlls, ops, englishDll =>
            {
                Console.WriteLine($"Validating strings for ${englishDll}");
                var translatedDlls = GetTranslatedDlls(extractedVsixPath, Path.GetFileNameWithoutExtension(englishDll));

                foreach (var translatedDll in translatedDlls)
                {
                    try
                    {
                        if (!CompareAllStrings(englishDll, translatedDll))
                        {
                            Console.WriteLine($@"Mismatch between {englishDll} and {translatedDll}.");
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });

            LogErrors(logPath);
            CleanExtractedFiles(extractedVsixPath);
        }

        private static string[] GetEnglishDlls(string extractedVsixPath)
        {
            return Directory.GetFiles(extractedVsixPath, "NuGet.*.dll");
        }

        private static string[] GetTranslatedDlls(string extractedVsixPath, string englishDllName)
        {
            return Directory.GetFiles(extractedVsixPath, $"{englishDllName}.resources.dll", SearchOption.AllDirectories);
        }

        private static bool CompareAllStrings(string firstDll, string secondDll)
        {
            var firstAssembly = Assembly.LoadFrom(firstDll);

            var firstAssemblyResources = firstAssembly
                .GetManifestResourceNames()
                .Where(r => r.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) && !r.EndsWith("g.resources", StringComparison.OrdinalIgnoreCase));

            var secondAssembly = Assembly.LoadFrom(secondDll);

            var secondAssemblyResources = secondAssembly
                .GetManifestResourceNames()
                .Where(r => r.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) && !r.EndsWith("g.resources", StringComparison.OrdinalIgnoreCase));

            foreach(var firstAssemblyResource in firstAssemblyResources)
            {

                var firstAssemblyResourceName = firstAssemblyResource
                    .Substring(0, firstAssemblyResource.LastIndexOf(".resource", StringComparison.OrdinalIgnoreCase));

                var secondAssemblyResource = secondAssemblyResources
                    .First(r => r.StartsWith(firstAssemblyResourceName));

                var secondAssemblyResourceName = secondAssemblyResource
                    .Substring(0, secondAssemblyResource.LastIndexOf(".resource", StringComparison.OrdinalIgnoreCase));


                var firstResourceManager = new ResourceManager(firstAssemblyResourceName, firstAssembly);
                var secondResourceManager = new ResourceManager(secondAssemblyResourceName, secondAssembly);

                var firstResourceSet = firstResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
                var secondResourceSet = secondResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);

                var firstResourceSetEnumerator = firstResourceSet.GetEnumerator();

               while(firstResourceSetEnumerator.MoveNext())
                {
                    if ((firstResourceSetEnumerator.Key is string) && (firstResourceSetEnumerator.Value is string))
                    {
                        var secondResource = secondResourceSet.GetString(firstResourceSetEnumerator.Key as string);
                        if (secondResource == null)
                        {
                            var error = $"Resource '{firstResourceSetEnumerator.Key}' from english resource set '{firstAssemblyResource}' "+
                                $"NOT FOUND in '{secondAssemblyResourceName}' in dll '{secondDll}'{Environment.NewLine}"+
                                $"'{firstResourceSetEnumerator.Key}':'{firstResourceSetEnumerator.Value}'{Environment.NewLine}"+
                                "================================================================================================================";
                            _errors.Enqueue(error);
                            return false;
                        }
                        else if (!CompareStrings(firstResourceSetEnumerator.Value as string, secondResource))
                        {
                            var error = $"Resource '{firstResourceSetEnumerator.Key}' from english resource set '{firstAssemblyResource}' "+
                                $"NOT SAME as {secondAssemblyResourceName} in dll {secondDll}{Environment.NewLine}"+
                                $"'{firstResourceSetEnumerator.Key}':'{firstResourceSetEnumerator.Value}' {Environment.NewLine}"+
                                $"'{firstResourceSetEnumerator.Key}':'{secondResource}'{Environment.NewLine}"+
                                "================================================================================================================";
                            _errors.Enqueue(error);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool CompareStrings(string firstString, string secondString)
        {
            var firstStringMetadata = GetStringMetadata(firstString);
            var secondStringMetadata = GetStringMetadata(secondString);
            return StringMetadataEquals(firstStringMetadata, secondStringMetadata);
        }

        private static Dictionary<string, int> GetStringMetadata(string str)
        {
            var result = new Dictionary<string, int>();
            var current = new StringBuilder();
            var i = 0;
            while (i < str.Length-1)
            {
                if(str[i] == '{' && str[i+1] == '{')
                {
                    i += 2;
                }
                if (str[i] == '{' && str[i+1] != '{')
                {
                    var closingIndex = str.IndexOf('}', i);
                    if (closingIndex == -1)
                    {
                        var pacleHolderString = str.Substring(i);
                        AddResult(result, pacleHolderString);

                        return result;
                    }
                    else if(closingIndex < str.Length-1 && str[closingIndex+1] == '}')
                    {
                        i += 2;
                    }
                    else
                    {
                        var pacleHolderString = str.Substring(i, closingIndex - i + 1);
                        AddResult(result, pacleHolderString);

                        i = closingIndex + 1;
                    }

                }
                else
                {
                    i += 1;
                }
            }
            return result;
        }

        private static void AddResult(Dictionary<string, int> result, string placeHolderString)
        {
            if (result.ContainsKey(placeHolderString))
            {
                result[placeHolderString]++;
            }
            else
            {
                result.Add(placeHolderString, 0);
            }
        }

        private static bool StringMetadataEquals(Dictionary<string, int> firstMetadata, Dictionary<string, int> secondMetadata)
        {
            var unequalMetadata = firstMetadata
                .Where(entry => !secondMetadata.ContainsKey(entry.Key) || secondMetadata[entry.Key] != entry.Value);
            return unequalMetadata.Count() == 0;
        }

        private static void LogErrors(string path)
        {
            Console.WriteLine($"Total error count: {_errors.Count()}");
            Console.WriteLine($"Errors logged at: {path}");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (StreamWriter w = File.AppendText(path))
            {
                foreach(var error in _errors)
                {
                    w.WriteLine(error);
                }
            }
        }

        private static void CleanExtractedFiles(string path)
        {
            Console.WriteLine("Cleaning up the extracted files");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        private static void ExtractVsix(string vsixPath, string extractedVsixPath)
        {
            Console.WriteLine($"Extracting {vsixPath} to {extractedVsixPath}");

            ZipFile.ExtractToDirectory(vsixPath, extractedVsixPath);

            Console.WriteLine($"Done Extracting...");
        }
    }
}
