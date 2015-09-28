using System.Web.Mvc;

namespace NuGet.Gallery.Staging.Web
{
    public class AppStart_ViewEngines
    {
        public static void Register(ViewEngineCollection viewEngines)
        {
            viewEngines.Clear();
            viewEngines.Add(new RazorViewEngine());
        }
    }
}