using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using NuGet.Gallery.Staging.Web.Code;
using NuGet.Gallery.Staging.Web.Code.Api;
using NuGet.Gallery.Staging.Web.Code.Mvc;
using NuGet.Gallery.Staging.Web.ViewModels;

namespace NuGet.Gallery.Staging.Web.Controllers
{
    [Authorize]
    public class PackageController 
        : BaseController
    {
        private readonly StageClient _stageClient;

        public PackageController()
        {
            _stageClient = new StageClient(ConfigurationManager.ConnectionStrings["StagingConnection"].ConnectionString);
        }
        
        public async Task<ActionResult> Add(string id)
        {
            if (await _stageClient.Exists(User.Identity.Name, id) == false)
            {
                return HttpNotFound();
            }

            return View(new UploadPackageViewModel(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Add(HttpPostedFileBase packageFile, string id)
        {
            if (await _stageClient.Exists(User.Identity.Name, id) == false)
            {
                return HttpNotFound();
            }

            var returnUrl = Url.Action("Details", "Stage", new { id = id });
            if (await _stageClient.UploadPackage(User.Identity.Name, id, packageFile.InputStream))
            {
                SetUiMessage(UiMessageTypes.Info, "The package was uploaded.");
            }
            else
            {
                SetUiMessage(UiMessageTypes.Error, "The package could not be uploaded.");

                returnUrl = Url.Action("Add", new { id = id });
            }
            
            if (Request.IsAjaxRequest())
            {
                return Json(returnUrl);
            }

            return Redirect(returnUrl);
        }
        
        public async Task<ActionResult> Delete(string id, string packageId, string packageVersion)
        {
            if (await _stageClient.Exists(User.Identity.Name, id) == false)
            {
                return HttpNotFound();
            }

            if (await _stageClient.DeletePackageVersion(User.Identity.Name, id, packageId, packageVersion))
            {
                SetUiMessage(UiMessageTypes.Info, "The package was deleted.");
            }
            else
            {
                SetUiMessage(UiMessageTypes.Error, "The package could not be deleted.");
            }

            return RedirectToAction("Details", "Stage", new { id = id });
        }
    }
}