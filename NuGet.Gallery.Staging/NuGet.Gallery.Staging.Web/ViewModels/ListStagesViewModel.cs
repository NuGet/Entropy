using System.Collections.Generic;
using NuGet.Gallery.Staging.Web.Code.Api;

namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class ListStagesViewModel
    {
        public string ApiKey { get; set; }
        public List<Stage> Stages { get; set; }

        public ListStagesViewModel(string apiKey, List<Stage> stages)
        {
            ApiKey = apiKey;
            Stages = stages;
        }
    }
}