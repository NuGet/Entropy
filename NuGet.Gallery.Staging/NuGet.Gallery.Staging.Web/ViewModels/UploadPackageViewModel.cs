namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class UploadPackageViewModel
    {
        public UploadPackageViewModel(string stageName)
        {
            StageName = stageName;
        }
        
        public string StageName { get; set; }
    }
}