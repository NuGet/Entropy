using System.Web.Mvc;
using NuGet.Gallery.Staging.Web.Code.Mvc;

namespace NuGet.Gallery.Staging.Web.Controllers
{
    public class HomeController 
        : BaseController
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Stage");
        }
    }
}