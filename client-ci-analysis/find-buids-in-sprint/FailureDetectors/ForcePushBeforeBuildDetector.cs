using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class ForcePushBeforeBuildDetector : IFailureDetector
    {
        public string FailureReason => "Git force push before build";

        public async Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(t => t.parentId == job.id)
                .OrderBy(t => t.order)
                .ToList();

            var failedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (failedTask?.issues?.Count > 0 && failedTask.issues[0].message.StartsWith("Git checkout failed"))
            {
                using (var logFile = await httpManager.GetAsync(failedTask.log.url))
                using (var reader = new StreamReader(logFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var message = line.Length > 29 ? line.Substring(29) : line;
                        if (message.StartsWith("fatal: reference is not a tree:"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
