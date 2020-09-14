using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class EndToEndTestRunFailureDetector : IFailureDetector
    {
        public string FailureReason => "End to end tests failed to generate results file";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(t => t.parentId == job.id)
                .OrderBy(t => t.order)
                .ToList();

            var probableFailedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (probableFailedTask != null)
            {
                if (probableFailedTask.issues != null && probableFailedTask.issues.Count > 0 && probableFailedTask.issues[0].message.StartsWith("RealTimeLogResults : Run Failed - Results.html did not get created"))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
    }
}
