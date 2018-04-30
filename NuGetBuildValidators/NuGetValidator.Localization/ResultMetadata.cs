
using Newtonsoft.Json.Linq;

namespace NuGetValidator.Localization
{
    public class ResultMetadata
    {
        public ResultType Type { get; set; }

        public string Description { get; set; }

        public int ErrorCount { get; set; }

        public string Path { get; set; }

        public JObject ToJson()
        {
            return new JObject
            {
                ["Type"] = Type.ToString(),
                ["Description"] = Description,
                ["ErrorCount"] = ErrorCount,
                ["Path"] = Path
            };
        }
    }
}
