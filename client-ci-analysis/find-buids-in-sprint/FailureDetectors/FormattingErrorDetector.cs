using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class FormattingErrorDetector : IFailureDetector
    {
        public string FailureReason => "Formatting error";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(t => t.parentId == job.id)
                .OrderBy(t => t.order)
                .ToList();

            var failedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (failedTask?.name == "Check source file format")
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
