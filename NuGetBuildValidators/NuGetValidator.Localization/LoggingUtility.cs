using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NuGetValidator.Localization
{
    internal static class LoggingUtility
    {
        public static void LogErrors(
            string logPath,
            IEnumerable<StringCompareResult> identicalLocalizedStrings,
            IEnumerable<StringCompareResult> mismatchErrors,
            IEnumerable<StringCompareResult> missingLocalizedErrors,
            IEnumerable<StringCompareResult> lockedStrings,
            Dictionary<string, Dictionary<string, List<string>>> nonLocalizedStringDeduped,
            Dictionary<string, LocalizedAssemblyResult> localizedDlls,
            ResultSummary resultSummary)
        {
            if (!Directory.Exists(logPath))
            {
                Console.WriteLine($"INFO: Creating new directory for logs at '{logPath}'");
                Directory.CreateDirectory(logPath);
            }

            var result = LogErrors(
                logPath,
                identicalLocalizedStrings,
                ResultType.NonLocalizedStrings,
                "These Strings are same as English strings.");

            // no lock needed as the execution is no longer parallel
            resultSummary.Results.Add(result);

            result = LogErrors(
                logPath,
                mismatchErrors,
                ResultType.MismatchStrings,
                "These Strings do not contain the same number of placeholders as the English strings.");

            resultSummary.Results.Add(result);

            result = LogErrors(
                logPath,
                missingLocalizedErrors,
                ResultType.MissingStrings,
                "These Strings are missing in the localized dlls.");

            resultSummary.Results.Add(result);

            result = LogErrors(
                logPath,
                lockedStrings,
                ResultType.LockedStrings,
                "These are wholly locked or contain a locked sub string.");

            resultSummary.Results.Add(result);

            result = LogNonLocalizedStringsDedupedErrors(
                logPath,
                nonLocalizedStringDeduped,
                ResultType.NonLocalizedStringsPerLanguage,
                "These Strings are same as English strings.");

            resultSummary.Results.Add(result);

            result = LogNonLocalizedAssemblyErrors(
                logPath,
                localizedDlls,
                ResultType.NonLocalizedAssemblies,
                "These assemblies have not been localized in one or more languages.");

            resultSummary.Results.Add(result);

            if (resultSummary.ExecutionType == ExecutionType.Vsix)
            {
                result = LogWrongLocalizedAssemblyPathErrors(
                    logPath,
                    localizedDlls,
                    ResultType.WrongLocalizedAssemblyPaths,
                    "These assemblies do not have localized dlls at the expected locations.");

                resultSummary.Results.Add(result);
            }

            LogResultSummary(
                logPath,
                resultSummary,
                "ResultSummary",
                "Summary of the localization validation.");
        }

        private static ResultMetadata LogErrors(
            string logPath,
            IEnumerable<StringCompareResult> errors,
            ResultType errorType,
            string errorDescription)
        {
            var result = new ResultMetadata()
            {
                Type = errorType,
                Description = errorDescription,
                ErrorCount = 0
            };

            if (errors.Any())
            {
                var path = Path.Combine(logPath, errorType + ".json");
                result.ErrorCount = errors.Count();
                result.Path = path;

                Console.WriteLine("================================================================================================================");
                Console.WriteLine($"Type: {errorType} - {errorDescription}");
                Console.WriteLine($"Count: {errors.Count()}");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine("================================================================================================================");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (StreamWriter file = File.AppendText(path))
                {
                    var array = new JArray();
                    foreach (var error in errors)
                    {
                        array.Add(error.ToJson());
                    }

                    var json = new JObject
                    {
                        ["Type"] = errorType.ToString(),
                        ["Description"] = errorDescription,
                        ["errors"] = array
                    };

                    var settings = new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented
                    };

                    var serializer = JsonSerializer.Create(settings);
                    serializer.Serialize(file, json);
                }
            }

            return result;
        }
        private static ResultMetadata LogWrongLocalizedAssemblyPathErrors(
            string logPath,
            Dictionary<string, LocalizedAssemblyResult> collection,
            ResultType errorType,
            string errorDescription)
        {
            var result = new ResultMetadata()
            {
                Type = errorType,
                Description = errorDescription,
                ErrorCount = 0
            };

            // log errors for when the assembly is not localized in expected languages and at expected paths
            var errors = collection.Keys.Where(key => !collection[key].HasExpectedLocalizedAssemblies());
            if (errors.Any())
            {
                var path = Path.Combine(logPath, errorType + ".json");
                result.ErrorCount = collection.Keys.Count;
                result.Path = path;

                Console.WriteLine("================================================================================================================");
                Console.WriteLine($"Type: {errorType} - {errorDescription}");
                Console.WriteLine($"Count: {errors.Count()}");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine("================================================================================================================");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (StreamWriter file = File.AppendText(path))
                {
                    var array = new JArray();
                    foreach (var error in errors)
                    {
                        array.Add(collection[error].ToJson());
                    }

                    var json = new JObject
                    {
                        ["Type"] = errorType.ToString(),
                        ["Description"] = errorDescription,
                        ["errors"] = array
                    };

                    var settings = new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented
                    };

                    var serializer = JsonSerializer.Create(settings);
                    serializer.Serialize(file, json);
                }
            }

            return result;
        }

        private static void LogResultSummary(
            string logPath,
            ResultSummary resultSummary,
            string fileName,
            string fileDescription)
        {
            // log result summary on console and to file
            var path = Path.Combine(logPath, fileName + ".json");

            Console.WriteLine("================================================================================================================");
            Console.WriteLine($"Type: {fileName} - {fileDescription}");
            Console.WriteLine($"Exitcode: {resultSummary.ExitCode}");
            Console.WriteLine($"Path: {path}");
            Console.WriteLine("================================================================================================================");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = resultSummary.ToJson();
            using (StreamWriter file = File.AppendText(path))
            {
                var settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                };

                var serializer = JsonSerializer.Create(settings);
                serializer.Serialize(file, json);
            }
        }


        private static ResultMetadata LogNonLocalizedStringsDedupedErrors(
            string logPath,
            Dictionary<string, Dictionary<string, List<string>>> collection,
            ResultType errorType,
            string errorDescription)
        {
            var result = new ResultMetadata()
            {
                Type = errorType,
                Description = errorDescription,
                ErrorCount = 0
            };

            if (collection.Keys.Any())
            {
                var path = Path.Combine(logPath, errorType + ".csv");
                result.ErrorCount = collection.Keys.Count;
                result.Path = path;

                Console.WriteLine("================================================================================================================");
                Console.WriteLine($"Type: {errorType} - {errorDescription}");
                Console.WriteLine($"Count: {collection.Keys.Count}");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine("================================================================================================================");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (StreamWriter w = File.AppendText(path))
                {
                    w.WriteLine("Dll Name, Resource Name, cs, de, es, fr, it, ja, ko, pl, pt-br, ru, tr, zh-hans, zh-hant");
                    foreach (var dll in collection.Keys)
                    {
                        foreach (var resource in collection[dll].Keys)
                        {
                            var line = new StringBuilder();
                            line.Append(dll);
                            line.Append(",");
                            line.Append(resource);
                            line.Append(",");
                            foreach (var language in LocaleUtility.LocaleStrings)
                            {
                                line.Append(collection[dll][resource].Contains(language) ? "Error" : "");
                                line.Append(",");
                            }

                            w.WriteLine(line.ToString());
                        }
                    }
                }
            }

            return result;
        }

        private static ResultMetadata LogNonLocalizedAssemblyErrors(
            string logPath,
            Dictionary<string, LocalizedAssemblyResult> collection,
            ResultType errorType,
            string errorDescription)
        {
            var result = new ResultMetadata()
            {
                Type = errorType,
                Description = errorDescription,
                ErrorCount = 0
            };

            // log errors for when the assembly is not localized in enough languages
            var errors = collection.Keys.Where(key => !collection[key].HasAllLocales());
            if (errors.Any())
            {
                var path = Path.Combine(logPath, errorType + ".csv");
                result.ErrorCount = errors.Count();
                result.Path = path;

                Console.WriteLine("================================================================================================================");
                Console.WriteLine($"Type: {errorType} - {errorDescription}");
                Console.WriteLine($"Count: {errors.Count()}");
                Console.WriteLine($"Path: {path}");
                Console.WriteLine("================================================================================================================");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (StreamWriter w = File.AppendText(path))
                {
                    w.WriteLine("Dll Name, cs, de, es, fr, it, ja, ko, pl, pt-br, ru, tr, zh-hans, zh-hant");
                    foreach (var error in errors)
                    {
                        var assemblyLocales = collection[error].Locales;
                        var line = new StringBuilder();
                        line.Append(error);
                        line.Append(",");
                        foreach (var language in LocaleUtility.LocaleStrings)
                        {
                            line.Append(!assemblyLocales.Contains(language) ? "Error" : "");
                            line.Append(",");
                        }

                        w.WriteLine(line.ToString());
                    }
                }
            }

            return result;
        }
    }
}