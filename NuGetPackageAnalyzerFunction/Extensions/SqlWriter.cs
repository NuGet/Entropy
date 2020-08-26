using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzerFunction.Extensions
{
    public static class SqlWriter
    {
        private static string InsertQueryTemplate = @"Insert into 
                    DemoTable(
                        Id,
                        Version,
                        Created,
                        hasPrimarySignature,
                        primarySignatureType,
                        primarySignatureTimestampCertSubject,
                        primaryTimestampV1Count,
                        primaryTimestampV2Count,
                        hasCounterSignature,
                        counterSignatureTimestampCertSubject,
                        counterSignatureTimestampV1Count,
                        counterSignatureTimestampV2Count)
                    Values('{0}', '{1}', '{2}', {3}, '{4}', '{5}', {6}, {7}, {8}, '{9}', {10}, {11})";

        private static string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public static async Task AddRecordAsync(AnalyzedObject obj, ILogger log)
        {
            var str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
            try 
            {
                using (SqlConnection connection = new SqlConnection(str))
                {
                    connection.Open();
                    var insertQuery = string.Format(InsertQueryTemplate,
                        obj.Id,
                        obj.Version,
                        obj.Created.HasValue ? obj.Created.Value.ToString(TimeFormat) : null,
                        obj.hasPrimarySignature ? 1 : 0,
                        obj.primarySignatureType,
                        obj.primarySignatureTimestampCertSubject,
                        obj.primaryTimestampV1Count,
                        obj.primaryTimestampV2Count,
                        obj.hasCounterSignature ? 1 : 0,
                        obj.counterSignatureTimestampCertSubject,
                        obj.counterSignatureTimestampV1Count,
                        obj.counterSignatureTimestampV2Count);

                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        var rows = await cmd.ExecuteNonQueryAsync();
                        log.LogInformation($"Added Record for {obj.Id}/{obj.Version}!");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }
    }
}
