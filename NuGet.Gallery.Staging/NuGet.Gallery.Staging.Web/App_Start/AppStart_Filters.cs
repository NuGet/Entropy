using System.Web.Mvc;
using NuGet.Gallery.Staging.Web.Code.Mvc;

namespace NuGet.Gallery.Staging.Web
{
    public class AppStart_Filters
    {
        public static void Register(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AddMessageToViewDataAttribute());
        }
    }
}
