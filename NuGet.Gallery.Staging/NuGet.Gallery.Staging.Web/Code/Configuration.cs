using System.Configuration;

namespace NuGet.Gallery.Staging.Web.Code
{
    public static class Configuration
    {
        public static string BaseServiceUrl { get { return ConfigurationManager.AppSettings["Stage.BaseServiceUrl"]; } }
        public static string BaseApiUrl { get { return ConfigurationManager.AppSettings["Stage.BaseApiUrl"]; } }
    }
}