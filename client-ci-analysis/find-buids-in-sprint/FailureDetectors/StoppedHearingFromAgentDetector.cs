using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class StoppedHearingFromAgentDetector : IFailureDetector
    {
        public string FailureReason => "We stopped hearing from agent";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            if (job.issues != null && job.issues.Count > 0 &&  job.issues[0].message.StartsWith("We stopped hearing from agent"))
            {
                return Task.FromResult(true);
            }

            if (job.issues != null && job.issues.Count == 2 &&
                job.issues[0].message.StartsWith("The agent did not connect within the alloted time") &&
                job.issues[1].message.StartsWith("We stopped hearing from agent"))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
