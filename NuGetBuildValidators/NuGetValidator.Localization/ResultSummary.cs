using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NuGetValidator.Localization
{
    public class ResultSummary
    {
        public string ToolName { get; set; } = "NuGet.Validator";

        public string ToolVersion { get; set; } = "1.5.0.0";

        public ExecutionType ExecutionType { get; set; }

        public string VsixPath { get; set; }

        public string VsixExtractionPath { get; set; }

        public string ArtifactsPath { get; set; }

        public string OutputPath { get; set; }    

        public List<string> InputFiles { get; set; }

        public string LciDirectory { get; set; }

        public List<string> LciFiles { get; set; } = new List<string>();

        public List<EnglishAssemblyMetadata> EnglishAssemblies { get; set; } = new List<EnglishAssemblyMetadata>();

        public List<ResultMetadata> Results { get; set; } = new List<ResultMetadata>();

        public int ExitCode { get; set; }

        public JObject ToJson()
        {
            var json = new JObject
            {
                ["ToolName"] = ToolName,
                ["ToolVersion"] = ToolVersion,
                ["ExecutionType"] = ExecutionType.ToString(),
                ["LciDirectory"] = LciDirectory,
                ["OutputPath"] = OutputPath,
                ["ExitCode"] = ExitCode,
                ["LciFilesFound"] = new JArray(LciFiles),
                ["EnglishAssemblyCount"] = EnglishAssemblies.Count,
                ["EnglishAssemblies"] = new JArray(EnglishAssemblies.Select(m => m.ToJson())),
                ["Results"] = new JArray(Results.Select(r => r.ToJson()))
            };

            switch (ExecutionType)
            {
                case ExecutionType.Vsix:
                    json["VsixPath"] = VsixPath;
                    json["VsixExtractionPath"] = VsixExtractionPath;
                    break;
                case ExecutionType.Artifacts:
                    json["ArtifactsPath"] = ArtifactsPath;
                    break;
                case ExecutionType.Files:
                    json["InputFiles"] = new JArray(InputFiles);
                    break;
            }

            return json;
        }
    }
}
