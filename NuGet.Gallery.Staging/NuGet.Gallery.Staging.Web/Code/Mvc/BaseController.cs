using System.Globalization;
using System.Web.Mvc;

namespace NuGet.Gallery.Staging.Web.Code.Mvc
{
    public abstract class BaseController
        : Controller
    {
        protected override ITempDataProvider CreateTempDataProvider()
        {
            return new CookieTempDataProvider(HttpContext);
        }

        protected void SetUiMessage(string messageType, string message)
        {
            TempData["MessageType"] = messageType;
            TempData["Message"] = message;
        }

        protected void SetUiMessage(string messageType, string message, params object[] parameters)
        {
            SetUiMessage(messageType, string.Format(message, parameters));
        }
        
        protected CultureInfo DetermineClientLocale()
        {
            if (Request == null)
            {
                return null;
            }

            var languages = Request.UserLanguages;
            if (languages == null)
            {
                return null;
            }

            foreach (var language in languages)
            {
                var lang = language.ToLowerInvariant().Trim();
                try
                {
                    return CultureInfo.GetCultureInfo(lang);
                }
                catch (CultureNotFoundException)
                {
                }
            }

            foreach (var language in languages)
            {
                var lang = language.ToLowerInvariant().Trim();
                if (lang.Length > 2)
                {
                    var lang2 = lang.Substring(0, 2);
                    try
                    {
                        return CultureInfo.GetCultureInfo(lang2);
                    }
                    catch (CultureNotFoundException)
                    {
                    }
                }
            }

            return null;
        }
    }
}