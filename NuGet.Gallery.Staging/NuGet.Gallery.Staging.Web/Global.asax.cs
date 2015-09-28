using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace NuGet.Gallery.Staging.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            AppStart_Filters.Register(GlobalFilters.Filters);
            AppStart_Routing.Register(RouteTable.Routes);
            AppStart_Bundles.Register(BundleTable.Bundles);
            AppStart_ViewEngines.Register(ViewEngines.Engines);
        }
    }
}
