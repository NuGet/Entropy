using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;
using NuGet.Gallery.Staging.Web.Code;
using NuGet.Gallery.Staging.Web.Code.Api;
using NuGet.Gallery.Staging.Web.Code.Mvc;
using NuGet.Gallery.Staging.Web.ViewModels;

namespace NuGet.Gallery.Staging.Web.Controllers
{
    [Authorize]
    public class StageController 
        : BaseController
    {
        private readonly StageClient _stageClient;

        public StageController()
        {
            _stageClient = new StageClient(ConfigurationManager.ConnectionStrings["StagingConnection"].ConnectionString);
        }

        public async Task<ActionResult> Index()
        {
            var apiKey = await _stageClient.GetApiKey(User.Identity.Name);
            var stages = await _stageClient.List(User.Identity.Name);

            return View(new ListStagesViewModel(apiKey, stages));
        }

        public async Task<ActionResult> Details(string id)
        {
            if (await _stageClient.Exists(User.Identity.Name, id) == false)
            {
                return HttpNotFound();
            }

            var apiKey = await _stageClient.GetApiKey(User.Identity.Name);
            var stage = await _stageClient.Get(User.Identity.Name, id);

            return View(new StageDetailsViewModel(apiKey, stage));
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateStageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _stageClient.Create(User.Identity.Name, model.StageName))
            {
                SetUiMessage(UiMessageTypes.Info, "The stage was created.");

                return RedirectToAction("Index");
            }

            SetUiMessage(UiMessageTypes.Error, "The stage could not be created.");

            return View(model);
        }

        public ActionResult Delete(string id)
        {
            return View(new DeleteStageViewModel(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> Delete_Post(string id)
        {
            if (await _stageClient.Exists(User.Identity.Name, id) == false)
            {
                return HttpNotFound();
            }

            if (await _stageClient.Delete(User.Identity.Name, id))
            {
                SetUiMessage(UiMessageTypes.Info, "The stage was deleted.");

                return RedirectToAction("Index");
            }

            SetUiMessage(UiMessageTypes.Error, "The stage could not be deleted.");

            return View(new DeleteStageViewModel(id));
        }
    }
}