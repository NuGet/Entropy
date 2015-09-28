using System.Collections.Generic;

namespace NuGet.Gallery.Staging.Web.Code.Api
{
    public class Package
    {
        public Package()
        {
            Versions = new List<PackageVersion>();
        }

        public string Id { get; set; }
        public List<PackageVersion> Versions { get; set; }
    }
}