using System.Web.Mvc;

namespace NuGet.Gallery.Staging.Web.Code.Mvc
{
    public sealed class AddMessageToViewDataAttribute
        : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Check if a message parameter is present
            if (filterContext.Controller.TempData["Message"] != null)
            {
                var messageType = filterContext.Controller.TempData["MessageType"].ToString();
                if (string.IsNullOrWhiteSpace(messageType))
                {
                    messageType = UiMessageTypes.Info;
                }
                filterContext.Controller.ViewData["MessageType"] = messageType;

                var message = filterContext.Controller.TempData["Message"].ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    filterContext.Controller.ViewData["Message"] = message;
                }
            }
        }
    }
}