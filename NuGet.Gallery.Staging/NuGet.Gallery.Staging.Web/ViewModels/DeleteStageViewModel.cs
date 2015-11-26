namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class DeleteStageViewModel
    {
        public DeleteStageViewModel(string stageName)
        {
            StageName = stageName;
        }

        public string StageName { get; set; }
    }
}