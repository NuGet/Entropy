using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace StagingWebApi.Resources
{
    public class V3RegistrationResource : StageResourceBase
    {
        public V3RegistrationResource(string ownerName, string stageId)
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

        public async Task<IDictionary<string, PackageDetails>> GetPackageDetails()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
                    SELECT StagePackage.[Id], StagePackage.[Version]
                    FROM StagePackage
                    INNER JOIN Stage ON StagePackage.StageKey = Stage.[Key]
                    INNER JOIN StageOwner ON Stage.[Key] = StageOwner.StageKey
                    INNER JOIN Owner ON Owner.[Key] = StageOwner.OwnerKey
                    WHERE Owner.Name = @OwnerName
                      AND Stage.Name = @StageName
                ", connection);

                command.Parameters.AddWithValue("OwnerName", OwnerName);
                command.Parameters.AddWithValue("StageName", StageName);

                SqlDataReader reader = await command.ExecuteReaderAsync();

                IDictionary<string, PackageDetails> result = new Dictionary<string, PackageDetails>(StringComparer.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    string id = reader.GetString(0);
                    string version = reader.GetString(1);

                    PackageDetails packageDetails;
                    if (!result.TryGetValue(id, out packageDetails))
                    {
                        packageDetails = new PackageDetails(id);
                        result.Add(id, packageDetails);
                    }

                    packageDetails.Versions.Add(version);
                }

                return result;
            }
        }
    }
}