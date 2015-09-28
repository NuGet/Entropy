using System.Web.Optimization;

namespace NuGet.Gallery.Staging.Web
{
    public class AppStart_Bundles
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void Register(BundleCollection bundles)
        {
            BundleTable.Bundles.Add(new ScriptBundle("~/bundles/scripts/nuget").Include(
                "~/Scripts/jquery-1.11.3.js",
                "~/Scripts/jquery.validate.js",
                "~/Scripts/jquery.validate.unobtrusive.js",
                "~/Scripts/modernizr-2.8.3.js",
                "~/Scripts/gallery.js"));

            BundleTable.Bundles.Add(new StyleBundle("~/bundles/styles/nuget").Include(
                "~/css/bootstrap-grid-layout.css",
                "~/css/nuget.css",
                "~/css/nuget.gallery.css"));
        }
    }
}
