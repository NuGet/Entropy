
using Newtonsoft.Json.Linq;

namespace NuGetValidator.Localization
{
    internal class StringCompareResult
    {
        public string ResourceName { get; set; }

        public string AssemblyName { get; set; }

        public Locale Locale { get; set; }

        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["ResourceName"] = ResourceName,
                ["AssemblyName"] = AssemblyName,
                ["Locale"] = Locale.ToString()
            };
        }
    }
}
