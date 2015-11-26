<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System.Web.Configuration;

namespace StagingWebApi.Resources
{
    public class V3Resource : StageResourceBase
    {
        public V3Resource(string ownerName, string stageId)
            : base(ownerName, stageId)
        {
<<<<<<< HEAD
            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
=======
            var rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
            ConnectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["PackageStaging"].ConnectionString;
        }

        public string ConnectionString
        {
            get;
            set;
        }

        public async Task<Uri> GetPackageBaseAddress()
        {
            Uri serviceBase = await GetServiceBase();
            if (serviceBase == null)
            {
                return null;
            }

            Uri address = await Utils.GetService(serviceBase, "PackageBaseAddress/3.0.0");
            return (address == null) ? null : address;
        }

        public async Task<Uri> GetSearchQueryService()
        {
            Uri serviceBase = await GetServiceBase();
            if (serviceBase == null)
            {
                return null;
            }

            Uri address = await Utils.GetService(serviceBase, "SearchQueryService/3.0.0-beta");
            return (address == null) ? null : address;
        }

        async Task<Uri> GetServiceBase()
        {
<<<<<<< HEAD
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
=======
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var command = new SqlCommand(@"
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
                    SELECT Stage.BaseService
                    FROM Stage
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                string result = (string)await command.ExecuteScalarAsync();

                if (result == null)
                {
                    return null;
                }

                return new Uri(result);
            }
        }
    }
}