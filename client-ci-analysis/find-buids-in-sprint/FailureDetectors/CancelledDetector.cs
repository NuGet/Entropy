using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class CancelledDetector : IFailureDetector
    {
        public string FailureReason => "Build canceled";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            if (job.issues != null && job.issues.Count > 0 && job.issues[0].message.StartsWith("The build was canceled by"))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
