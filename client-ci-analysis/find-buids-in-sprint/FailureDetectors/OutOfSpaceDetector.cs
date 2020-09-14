using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class OutOfSpaceDetector : IFailureDetector
    {
        public string FailureReason => "Out of disk space";

        public Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            if (job.issues != null && job.issues.Count > 0 && 
                (job.issues[0].message.StartsWith("There is not enough space on the disk") ||
                 job.issues[0].message.StartsWith("System.IO.IOException: There is not enough space on the disk")))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
