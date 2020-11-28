using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class TimeoutDetector : IFailureDetector
    {
        private static readonly Regex timeoutRegex = new Regex(@"The job running on agent [\w-]+ ran longer than the maximum time of \d+ minutes.");

        public string FailureReason => "job timeout";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            if (job.result == "canceled")
            {
                if (job.issues.Any(i => timeoutRegex.IsMatch(i.message)))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
    }
}
