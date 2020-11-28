using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class GitSubmoduleUpdateFailedDetector : IFailureDetector
    {
        public string FailureReason => "Git submodule failed";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(t => t.parentId == job.id)
                .OrderBy(t => t.order)
                .ToList();

            var failedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (failedTask != null && failedTask.issues != null && failedTask.issues.Count > 0 && failedTask.issues[0].message.StartsWith("Git submodule update failed"))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
