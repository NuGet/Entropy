using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(NuGet.Gallery.Staging.Web.Startup))]
namespace NuGet.Gallery.Staging.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AppStart_Authentication.Configure(app);
        }
    }
}
