using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace StagingWebApi.Resources
{
    public class V3Resource : StageResourceBase
    {
        public V3Resource(string ownerName, string stageId)
            : base(ownerName, stageId)
        {
            Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/StagingWebApi");
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
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
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