using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class BuildErrorDetector : IFailureDetector
    {
        private static readonly Regex regex = new Regex(@"^(?<filename>[\w\\\.]+)\(\d+,\d+\): Error (?<code>[\w\d]+):");

        public string FailureReason => "Compile error";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(t => t.parentId == job.id)
                .OrderBy(t => t.order)
                .ToList();

            var failedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (failedTask?.issues != null)
            {
                foreach (var issue in failedTask.issues)
                {
                    var match = regex.Match(issue.message);
                    if (match.Success)
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }
    }
}
