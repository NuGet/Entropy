using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using NuGet.Gallery.Staging.Web.Code;
using NuGet.Gallery.Staging.Web.Code.Api;
using NuGet.Gallery.Staging.Web.Code.Mvc;
using NuGet.Gallery.Staging.Web.ViewModels;

namespace NuGet.Gallery.Staging.Web.Controllers
{
    [Authorize]
    public class AccountController 
        : BaseController
    {
        private readonly AuthenticationService _authenticationService;
        private readonly StageClient _stageClient;

        public AccountController()
        {
            _authenticationService = new AuthenticationService(ConfigurationManager.ConnectionStrings["GalleryConnection"].ConnectionString);
            _stageClient = new StageClient(ConfigurationManager.ConnectionStrings["StagingConnection"].ConnectionString);
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authenticationService.AuthenticateAsync(model.Username, model.Password);
            if (user != null)
            {
                // Ensure stages can be created
                await _stageClient.EnsureOwnerCreated(user.Username);

                // Login
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Username));
                claims.Add(new Claim(ClaimTypes.Name, user.Username));
                claims.Add(new Claim(ClaimTypes.Email, user.EmailAddress));

                var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                
                var authenticationManager = Request.GetOwinContext().Authentication;
                authenticationManager.SignIn(identity);

                return RedirectToLocal(returnUrl);
            }
           
            SetUiMessage(UiMessageTypes.Error, "The username or password is invalid.");

            return RedirectToAction("Login", new { returnUrl = returnUrl });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            var authenticationManager = Request.GetOwinContext().Authentication;
            authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }
        
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}