using Newtonsoft.Json.Linq;

namespace NuGetValidator.Localization
{
    internal class LockedStringResult : StringCompareResult
    {
        public string EnglishValue { get; set; }

        public string LockComment { get; set; }

        public override JObject ToJson()
        {
            return new JObject
            {
                ["ResourceName"] = ResourceName,
                ["AssemblyName"] = AssemblyName,
                ["EnglishValue"] = EnglishValue,
                ["LockComment"] = LockComment,
            };
        }
    }
}
