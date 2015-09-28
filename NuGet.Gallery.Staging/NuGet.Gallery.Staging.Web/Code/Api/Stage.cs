using System.Collections.Generic;

namespace NuGet.Gallery.Staging.Web.Code.Api
{
    public class Stage
    {
        public Stage()
        {
            Sources = new Dictionary<string, string>();
            Packages = new List<Package>();
        }

        public string Name { get; set; }
        public Dictionary<string, string> Sources { get; set; }
        public List<Package> Packages { get; set; }
    }
}