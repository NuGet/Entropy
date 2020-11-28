using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal interface IFailureDetector
    {
        Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager);
        string FailureReason { get; }
    }
}
