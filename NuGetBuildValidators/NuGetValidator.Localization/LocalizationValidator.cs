using NuGetValidator.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetValidator.Localization
{
    public class LocalizationValidator
    {
        /// <summary>
        /// Strings for which the localization is same as english
        /// </summary>
        private static readonly ConcurrentQueue<StringCompareResult> _identicalLocalizedStrings = new ConcurrentQueue<StringCompareResult>();

        /// <summary>
        /// strings that have are missing in localized assemblies
        /// </summary>
        private static readonly ConcurrentQueue<StringCompareResult> _missingLocalizedErrors = new ConcurrentQueue<StringCompareResult>();

        /// <summary>
        /// strings that have different number of placeholders in the localized value
        /// </summary>
        private static readonly ConcurrentQueue<StringCompareResult> _mismatchErrors = new ConcurrentQueue<StringCompareResult>();

        /// <summary>
        /// strings that are not localized saved per dll
        /// dll name -> resource name -> list of locales
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, List<string>>> _identicalLocalizedStringDeduped = new Dictionary<string, Dictionary<string, List<string>>>();

        /// <summary>
        /// dlls and all the languages in which they have been localized 
        /// dll name -> list of locales
        /// </summary>
        private static readonly Dictionary<string, LocalizedAssemblyResult> _localizedDlls = new Dictionary<string, LocalizedAssemblyResult>();

        /// <summary>
        /// strings that are locked per dll
        /// dll name -> resource name -> locked string
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, StringCompareResult>> _lockedStrings = new Dictionary<string, Dictionary<string, StringCompareResult>>();

        /// <summary>
        /// lock for _identicalLocalizedStringErrorsDeduped
        /// </summary>
        private static readonly object _identicalLocalizedStringsDedupedCollectionLock = new object();

        /// <summary>
        /// Summary object to hold all important information that will be written to a json file
        /// </summary>
        private static readonly ResultSummary _resultSummary = new ResultSummary();

        /// <summary>
        /// lock for _localizedDlls
        /// </summary>
        private static readonly object _localizedDllCollectionLock = new object();

        /// <summary>
        /// lock for _lockedStrings
        /// </summary>
        private static readonly object _lockedStringCollectionLock = new object();

        /// <summary>
        /// lock for _resultSummary
        /// </summary>
        private static readonly object _resultSummaryLock = new object();

        /// <summary>
        /// number of parallel threads
        /// </summary>
        private static int _numberOfThreads = 8;

        public static int ExecuteForVsix(string VsixPath, string VsixExtractPath, string OutputPath, string CommentsPath)
        {
            var vsixPath = VsixPath;
            var extractedVsixPath = VsixExtractPath;
            var logPath = OutputPath;
            var lciCommentsDirPath = CommentsPath;

            lock (_resultSummaryLock)
            {
                _resultSummary.ExecutionType = ExecutionType.Vsix;
                _resultSummary.VsixPath = VsixPath;
                _resultSummary.VsixExtractionPath = VsixExtractPath;
                _resultSummary.OutputPath = OutputPath;
            }

            WarnIfNoLciDirectory(lciCommentsDirPath);

            VsixUtility.ExtractVsix(vsixPath, extractedVsixPath);

            var englishDlls = FileUtility.GetDlls(extractedVsixPath);

            Execute(lciCommentsDirPath, englishDlls);

            var lockedStringsFalttened = _lockedStrings
                .SelectMany(d => d.Value.Select(s => s.Value))
                .ToList();

            var exitCode = GetExitCode();
            lock (_resultSummaryLock)
            {
                _resultSummary.ExitCode = exitCode;
            }

            LoggingUtility.LogErrors(
                logPath,
                _identicalLocalizedStrings,
                _mismatchErrors,
                _missingLocalizedErrors,
                lockedStringsFalttened,
                _identicalLocalizedStringDeduped,
                _localizedDlls,
                _resultSummary);

            return exitCode;
        }

        public static int ExecuteForArtifacts(string ArtifactsPath, string OutputPath, string CommentsPath)
        {
            var artifactsPath = ArtifactsPath;
            var logPath = OutputPath;
            var lciCommentsDirPath = CommentsPath;

            lock (_resultSummaryLock)
            {
                _resultSummary.ExecutionType = ExecutionType.Artifacts;
                _resultSummary.ArtifactsPath = artifactsPath;
                _resultSummary.OutputPath = OutputPath;
            }

            WarnIfNoLciDirectory(lciCommentsDirPath);

            var englishDlls = FileUtility.GetDlls(artifactsPath,
                                                  isArtifacts: true,
                                                  skipPathsContaining: "net45"); // net45 assemblies aren't localized, ignore them

            Execute(lciCommentsDirPath, englishDlls);

            var lockedStringsFalttened = _lockedStrings
                .SelectMany(d => d.Value.Select(s => s.Value))
                .ToList();

            var exitCode = GetExitCode();
            lock (_resultSummaryLock)
            {
                _resultSummary.ExitCode = exitCode;
            }

            LoggingUtility.LogErrors(
                logPath,
                _identicalLocalizedStrings,
                _mismatchErrors,
                _missingLocalizedErrors,
                lockedStringsFalttened,
                _identicalLocalizedStringDeduped,
                _localizedDlls,
                _resultSummary);

            return exitCode;
        }

        public static int ExecuteForFiles(IList<string> Files, string OutputPath, string CommentsPath)
        {
            var files = Files;
            var logPath = OutputPath;
            var lciCommentsDirPath = CommentsPath;

            lock (_resultSummaryLock)
            {
                _resultSummary.ExecutionType = ExecutionType.Files;
                _resultSummary.InputFiles.AddRange(files);
                _resultSummary.OutputPath = OutputPath;
            }

            WarnIfNoLciDirectory(lciCommentsDirPath);

            Execute(lciCommentsDirPath, files.ToArray());

            var lockedStringsFalttened = _lockedStrings
                .SelectMany(d => d.Value.Select(s => s.Value))
                .ToList();

            var exitCode = GetExitCode();
            lock (_resultSummaryLock)
            {
                _resultSummary.ExitCode = exitCode;
            }

            LoggingUtility.LogErrors(
                logPath,
                _identicalLocalizedStrings,
                _mismatchErrors,
                _missingLocalizedErrors,
                lockedStringsFalttened,
                _identicalLocalizedStringDeduped,
                _localizedDlls,
                _resultSummary);

            return exitCode;
        }

        private static void Execute(string lciCommentsDirPath, string[] englishDlls)
        {
            Console.WriteLine($"Total English Dlls: {englishDlls.Count()}");

            ParallelOptions ops = new ParallelOptions { MaxDegreeOfParallelism = _numberOfThreads };
            Parallel.ForEach(englishDlls, ops, englishDll =>
            {
                var englishAssemblyMetadata = new EnglishAssemblyMetadata()
                {
                    AssemblyPath = englishDll
                };

                if (!File.Exists(englishDll))
                {
                    Console.WriteLine($"File Not Found: {englishDll}");
                }
                else if (!englishDll.EndsWith(".dll") && !englishDll.EndsWith(".exe"))
                {
                    Console.WriteLine($"File Not a dll/exe: {englishDll}\n");
                }
                else
                {
                    if (DoesDllContainResourceStrings(englishDll))
                    {
                        var translatedDlls = GetTranslatedDlls(Path.GetDirectoryName(englishDll), englishDll);
                        englishAssemblyMetadata.HasResources = true;
                        englishAssemblyMetadata.TranslatedAssemblyCount = translatedDlls.Count();

                        Console.WriteLine($"Validating: {englishDll} " +
                            Environment.NewLine +
                            $"\t Contains resource strings: True" +
                            Environment.NewLine +
                            $"\t Translated assembly count: {translatedDlls.Count()}" +
                            Environment.NewLine);

                        var localizedAssemblyResult = new LocalizedAssemblyResult(englishDll, new HashSet<string>(translatedDlls.Select(s => s.ToLower()).ToList()));

                        // Add english dll to allow filtering
                        AddToLocalizedAssemblyCollection(
                            _localizedDlls,
                            englishDll,
                            localizedAssemblyResult);

                        foreach (var translatedDll in translatedDlls)
                        {
                            try
                            {
                                CompareAllStrings(englishDll, translatedDll, lciCommentsDirPath);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                    else
                    {
                        englishAssemblyMetadata.HasResources = false;
                        Console.WriteLine($"Validating: {englishDll} " +
                            Environment.NewLine +
                            $"\t Contains resource strings: False" +
                            Environment.NewLine);
                    }
                }

                lock (_resultSummaryLock)
                {
                    _resultSummary.EnglishAssemblies.Add(englishAssemblyMetadata);
                }
            });
        }

        private static void WarnIfNoLciDirectory(string lciCommentsDirPath)
        {
            var lciFiles = new List<string>();

            if (!Directory.Exists(lciCommentsDirPath))
            {
                Console.WriteLine($"WARNING: LCI comments path '{lciCommentsDirPath}' not found in local git repo! " +
                    "The reults will not contain any locked strings and the non localized string count will be higher.");
            }
            else
            {
                Console.WriteLine($"INFO: LCI Files found - ");
                foreach (var file in Directory.GetFiles(lciCommentsDirPath))
                {
                    lciFiles.Add(file);
                    Console.WriteLine($"\t {file}");
                }
            }

            lock (_resultSummaryLock)
            {
                _resultSummary.LciDirectory = lciCommentsDirPath;
                _resultSummary.LciFiles.AddRange(lciFiles);
            }
        }

        private static string[] GetTranslatedDlls(string rootDir, string englishDllPath)
        {
            var englishDllName = Path.GetFileNameWithoutExtension(englishDllPath);
            return Directory.GetFiles(rootDir, $"{englishDllName}.resources.dll", SearchOption.AllDirectories);
        }

        private static void CompareAllStrings(string firstDll, string secondDll, string lciCommentDirPath)
        {
            var lciFilePath = Path.Combine(lciCommentDirPath, Path.GetFileName(firstDll) + ".lci");
            XElement lciFile = null;
            if (File.Exists(lciFilePath))
            {
                lciFile = XElement.Load(lciFilePath);
            }
            else
            {
                Console.WriteLine($"WARNING: No LCI file found at {lciFilePath}");
            }

            var firstAssembly = Assembly.LoadFrom(firstDll);
            var firstAssemblyResourceFullNames = GetResourceFullNamesFromDll(firstAssembly);

            var secondAssembly = Assembly.LoadFrom(secondDll);
            var secondAssemblyResourceFullNames = GetResourceFullNamesFromDll(secondAssembly);

            var firstAssemblyName = Path.GetFileNameWithoutExtension(firstDll);
            Enum.TryParse<Locale>(Directory.GetParent(secondDll).Name, out var locale);

            foreach (var firstAssemblyResourceFullName in firstAssemblyResourceFullNames)
            {
                var firstResourceSetEnumerator = GetResourceEnumeratorFromAssembly(firstAssemblyResourceFullName, firstAssembly);

                while (firstResourceSetEnumerator.MoveNext())
                {
                    if (IsResourceAValidString(firstResourceSetEnumerator.Key, firstResourceSetEnumerator.Value))
                    {
                        if (IsResourceStringUriOrNonAlphabetical(firstResourceSetEnumerator.Key.ToString(), firstResourceSetEnumerator))
                        {
                            continue;
                        }

                        var lciEntries = GetLciEntries(lciFile, firstResourceSetEnumerator.Key.ToString());

                        if (lciEntries?.Any() == true)
                        {
                            var lciCommentAndValueTuple = GetLciCommentAndValueString(lciEntries);
                            var cmtString = lciCommentAndValueTuple.Item1;
                            var valueString = lciCommentAndValueTuple.Item2;

                            if (cmtString.Contains("Locked"))
                            {
                                var compareResult = new LockedStringResult()
                                {
                                    ResourceName = firstResourceSetEnumerator.Key.ToString(),
                                    AssemblyName = firstAssemblyName,
                                    EnglishValue = firstResourceSetEnumerator.Value.ToString(),
                                    LockComment = cmtString
                                };

                                AddToLockedStringCollection(
                                    _lockedStrings,
                                    firstAssemblyName,
                                    firstResourceSetEnumerator.Key.ToString(),
                                    compareResult);
                            }

                            if (IsStringResourceLocked(cmtString, valueString))
                            {
                                continue;
                            }
                        }

                        var secondResourceFullName = secondAssemblyResourceFullNames
                            .First(r => r.StartsWith(GetResourceNameFromFullName(firstAssemblyResourceFullName)));

                        var secondResource = GetResourceFromAssembly(
                            secondResourceFullName,
                            firstResourceSetEnumerator.Key.ToString(),
                            secondAssembly);

                        if (secondResource == null)
                        {
                            var compareResult = new StringCompareResult()
                            {
                                ResourceName = firstResourceSetEnumerator.Key.ToString(),
                                AssemblyName = firstAssemblyName,
                                Locale = locale,
                            };

                            _missingLocalizedErrors.Enqueue(compareResult);
                        }
                        else if (!CompareStrings(firstResourceSetEnumerator.Value.ToString(), secondResource))
                        {
                            var compareResult = new MismatchedStringResult()
                            {
                                ResourceName = firstResourceSetEnumerator.Key.ToString(),
                                AssemblyName = firstAssemblyName,
                                Locale = locale,
                                EnglishValue = firstResourceSetEnumerator.Value.ToString(),
                                LocalizedValue = secondResource
                            };

                            _mismatchErrors.Enqueue(compareResult);
                        }
                        else if (secondResource.Equals(firstResourceSetEnumerator.Value.ToString()))
                        {
                            var compareResult = new IdenticalStringResult()
                            {
                                ResourceName = firstResourceSetEnumerator.Key.ToString(),
                                AssemblyName = firstAssemblyName,
                                Locale = locale,
                                EnglishValue = firstResourceSetEnumerator.Value.ToString(),
                                LocalizedValue = secondResource
                            };

                            _identicalLocalizedStrings.Enqueue(compareResult);

                            AddToIdenticalStringsDedupedCollection(
                                _identicalLocalizedStringDeduped,
                                firstAssemblyName,
                                firstResourceSetEnumerator.Key.ToString(),
                                locale.ToString());
                        }
                    }
                }
            }
        }

        private static bool IsResourceStringUriOrNonAlphabetical(string resourceKey, IDictionaryEnumerator resourceSetEnumerator)
        {
            if ((Uri.TryCreate((resourceSetEnumerator.Value.ToString()), UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp)) ||
                (resourceSetEnumerator.Value.ToString()).All(c => !char.IsLetter(c)))
            {
                return true;
            }
            return false;
        }

        private static bool IsResourceAValidString(object resourceKey, object resourceValue)
        {
            if ((resourceKey is string) &&
                !((resourceKey.ToString()).StartsWith(">>")) &&
                (resourceValue is string))
            {
                return true;
            }
            return false;
        }

        private static IEnumerable<XElement> GetLciEntries(XElement lciFile, string resourceStringKey)
        {
            return lciFile
                ?.Descendants()
                .Where(d => d.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase))
                .Where(d => d.Attribute(XName.Get("ItemId")).Value.Equals(";" + resourceStringKey, StringComparison.OrdinalIgnoreCase));
        }

        private static Tuple<string, string> GetLciCommentAndValueString(IEnumerable<XElement> lciEntries)
        {
            var lciEntry = lciEntries.First();
            var valueData = lciEntry
                .Descendants()
                .Where(d => d.Name.LocalName.Equals("val", StringComparison.OrdinalIgnoreCase));
            var valueString = ((XCData)valueData.First().FirstNode).Value;

            var cmtData = lciEntry.Descendants()
                .Where(d => d.Name.LocalName.Equals("cmt", StringComparison.OrdinalIgnoreCase));
            var cmtString = ((XCData)cmtData.First().FirstNode).Value;

            return new Tuple<string, string>(cmtString, valueString);
        }

        public static bool IsStringResourceLocked(string cmtString, string valueString)
        {
            if (cmtString.Equals("{Locked}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                var lockedSubStrings = GetLockedSubStrings(cmtString);
                var valueStringCopy = string.Copy(valueString);
                foreach (var lockedSubString in lockedSubStrings)
                {
                    if (valueStringCopy.Contains(lockedSubString))
                    {
                        valueStringCopy = valueStringCopy.Replace(lockedSubString, string.Empty);
                    }
                }
                if (string.IsNullOrEmpty(valueStringCopy) || valueStringCopy.All(c => !char.IsLetter(c)))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> GetLockedSubStrings(string cmtString)
        {
            var lockedSubStrings = new List<string>();
            var commentStrings = GetStringResourceComments(cmtString);
            foreach (var commentString in commentStrings)
            {
                var commentStringSplit = commentString.Split('=');
                var type = commentStringSplit[0];
                var comments = commentStringSplit[1];
                if (type.Contains("Locked"))
                {
                    var subStrings = comments.Split(',');
                    subStrings.ToList().ForEach(s => lockedSubStrings.Add(CleanLockedString(s.Trim())));

                }
            }
            return lockedSubStrings;
        }

        private static string CleanLockedString(string lockedString)
        {
            if (lockedString.EndsWith("}"))
            {
                lockedString = lockedString.Substring(0, lockedString.Length - 1);
            }
            if (lockedString.StartsWith("\""))
            {
                lockedString = lockedString.Substring(1, lockedString.Length - 2);
            }
            if (lockedString.EndsWith("\""))
            {
                lockedString = lockedString.Substring(0, lockedString.Length - 2);
            }
            return lockedString;
        }

        private static IEnumerable<string> GetStringResourceComments(string cmtString)
        {
            var commentSubStrings = new List<string>();
            for (int i = 0; i < cmtString.Length; i++)
            {
                var ch = cmtString[i];
                if (ch == '{')
                {
                    var endLocation = cmtString.IndexOf('}', i);
                    commentSubStrings.Add(cmtString.Substring(i, endLocation - i + 1));
                }
            }
            return commentSubStrings;
        }

        private static bool DoesDllContainResourceStrings(string dll)
        {
            var assembly = Assembly.LoadFrom(dll);
            return GetResourceFullNamesFromDll(assembly).Any();
        }

        private static IEnumerable<string> GetResourceFullNamesFromDll(Assembly assembly)
        {
            return assembly
                .GetManifestResourceNames()
                .Where(r => r.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) &&
                           !r.EndsWith("g.resources", StringComparison.OrdinalIgnoreCase));
        }

        private static IDictionaryEnumerator GetResourceEnumeratorFromAssembly(string resourceFullName, Assembly assembly)
        {
            var assemblyResourceName = resourceFullName
                .Substring(0, resourceFullName.LastIndexOf(".resource", StringComparison.OrdinalIgnoreCase));

            var resourceManager = new ResourceManager(assemblyResourceName, assembly);

            var resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);

            return resourceSet.GetEnumerator();
        }

        private static ResourceSet GetResourceSetFromAssembly(string resourceFullName, Assembly assembly)
        {
            var assemblyResourceName = GetResourceNameFromFullName(resourceFullName);

            var resourceManager = new ResourceManager(assemblyResourceName, assembly);

            return resourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        }

        private static string GetResourceNameFromFullName(string resourceFullName)
        {
            return resourceFullName
                .Substring(0, resourceFullName.LastIndexOf(".resource", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetResourceFromAssembly(string resourceFullName, string resourceKey, Assembly assembly)
        {
            var resourceSet = GetResourceSetFromAssembly(resourceFullName, assembly);
            return resourceSet.GetString(resourceKey);
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
            while (i < str.Length - 1)
            {
                if (str[i] == '{' && str[i + 1] == '{')
                {
                    i += 2;
                }
                if (str[i] == '{' && str[i + 1] != '{')
                {
                    var closingIndex = str.IndexOf('}', i);
                    if (closingIndex == -1)
                    {
                        var pacleHolderString = str.Substring(i);
                        AddResult(result, pacleHolderString);

                        return result;
                    }
                    else if (closingIndex < str.Length - 1 && str[closingIndex + 1] == '}')
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

        private static void AddToLockedStringCollection(
            Dictionary<string, Dictionary<string, StringCompareResult>> collection,
            string dllName,
            string resourceName,
            StringCompareResult result)
        {
            lock (_lockedStringCollectionLock)
            {
                if (collection.ContainsKey(dllName))
                {
                    if (!collection[dllName].ContainsKey(resourceName))
                    {
                        collection[dllName][resourceName] = result;
                    }
                }
                else
                {
                    collection[dllName] = new Dictionary<string, StringCompareResult> { { resourceName, result } };
                }
            }
        }

        private static void AddToIdenticalStringsDedupedCollection(
            Dictionary<string, Dictionary<string, List<string>>> collection,
            string dllName,
            string resourceName,
            string language)
        {
            lock (_identicalLocalizedStringsDedupedCollectionLock)
            {
                if (collection.ContainsKey(dllName))
                {
                    if (collection[dllName].ContainsKey(resourceName))
                    {
                        collection[dllName][resourceName].Add(language.ToLower());
                    }
                    else
                    {
                        collection[dllName][resourceName] = new List<string> { language.ToLower() };
                    }
                }
                else
                {
                    collection[dllName] = new Dictionary<string, List<string>> { { resourceName, new List<string> { language.ToLower() } } };
                }
            }
        }

        private static void AddToLocalizedAssemblyCollection(
            Dictionary<string, LocalizedAssemblyResult> collection,
            string dllName,
            LocalizedAssemblyResult result)
        {
            lock (_localizedDllCollectionLock)
            {
                if (!collection.ContainsKey(dllName))
                {
                    collection[dllName] = result;
                }
            }
        }

        private static int GetExitCode()
        {
            int result = 0;

            if (_identicalLocalizedStrings.Any())
            {
                // Currently these are treated as non fatal errors
                result = result == 1 ? 1 : 0;
            }
            if (_mismatchErrors.Any())
            {
                // Currently these are treated as non fatal errors
                result = result == 1 ? 1 : 0;
            }
            if (_missingLocalizedErrors.Any())
            {
                // These are treated as fatal errors
                result = 1;
            }
            if (_identicalLocalizedStringDeduped.Keys.Any())
            {
                // Currently these are treated as non fatal errors
                result = result == 1 ? 1 : 0;
            }
            if (_localizedDlls.Keys.Any(key => !_localizedDlls[key].HasAllLocales()))
            {
                // These are treated as fatal errors
                result = 1;
            }
            if (_resultSummary.ExecutionType == ExecutionType.Vsix && 
                _localizedDlls.Keys.Any(key => !_localizedDlls[key].HasExpectedLocalizedAssemblies()))
            {
                // These are treated as fatal errors
                result = 1;
            }

            return result;
        }
    }
}
