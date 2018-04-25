using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGetValidator.Localization
{
    internal class LocalizedAssemblyResult
    {
        private HashSet<string> _expectedLocalizedAssemblies;
        private HashSet<string> _localeStrings;

        public string AssemblyName { get; }

        public string AssemblyPath { get; }

        public HashSet<string> LocalizedAssemblies { get; }

        public HashSet<string> ExpectedLocalizedAssemblies
        {
            get
            {
                if (_expectedLocalizedAssemblies == null)
                {
                    var root = Directory.GetParent(AssemblyPath).FullName;
                    _expectedLocalizedAssemblies = new HashSet<string>(LocaleUtility.LocaleStrings.Select(s => Path.Combine(root, s, $"{AssemblyName}.resources.dll").ToLower()));
                }

                return _expectedLocalizedAssemblies;
            }
        }

        public HashSet<string> Locales
        {
            get
            {
                if (_localeStrings == null)
                {
                    _localeStrings = new HashSet<string>(LocalizedAssemblies.Select(s => Directory.GetParent(s).Name));
                }

                return _localeStrings;
            }
        }

        public LocalizedAssemblyResult(string assemblyPath, HashSet<string> localizedAssemblies)
        {
            AssemblyPath = assemblyPath;
            AssemblyName = Path.GetFileNameWithoutExtension(AssemblyPath);
            LocalizedAssemblies = localizedAssemblies;
        }

        public bool HasAllLocales()
        {
            return !(LocalizedAssemblies.Count < LocaleUtility.LocaleStrings.Count());
        }

        public bool HasExpectedLocalizedAssemblies()
        {
            return LocalizedAssemblies.SetEquals(ExpectedLocalizedAssemblies);
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["AssemblyName"] = AssemblyName,
                ["AssemblyPath"] = AssemblyPath,
                ["LocalizedAssemblies"] = new JArray(LocalizedAssemblies),
                ["ExpectedLocalizedAssemblies"] = new JArray(ExpectedLocalizedAssemblies),
                ["MissingLocalizedAssemblies"] = new JArray(ExpectedLocalizedAssemblies.Except(LocalizedAssemblies))
            };
        }
    }
}
