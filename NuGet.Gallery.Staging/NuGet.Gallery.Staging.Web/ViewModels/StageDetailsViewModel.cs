using NuGet.Gallery.Staging.Web.Code.Api;

namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class StageDetailsViewModel
    {
        public string ApiKey { get; set; }
        public Stage Stage { get; set; }

        public StageDetailsViewModel(string apiKey, Stage stage)
        {
            ApiKey = apiKey;
            Stage = stage;
        }
    }
}