using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace StagingWebApi.Resources
{
    public class V3RegistrationResource : StageResourceBase
    {
        public V3RegistrationResource(string ownerName, string stageId)
            : base(ownerName, stageId)
        {
        }
    }
}