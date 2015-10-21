using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StagingWebApi
{
    public class StagePersistenceFactory
    {
        public IStagePersistence Create()
        {
            return new SqlStagePersistence();
        }
    }
}