using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StagingWebApi
{
    public class PackageDetails
    {
        public string Id { get; private set; }
        public List<string> Versions { get; private set; }

        public PackageDetails(string id)
        {
            Id = id;
            Versions = new List<string>();
        }
    }
}