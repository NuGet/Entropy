using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class JobCannotBeRunrunDetector : IFailureDetector
    {
        private static readonly Regex regex = new Regex(@"The file '(?<filename>[\\\/\w\.\d]+)' already exists.", RegexOptions.IgnorePatternWhitespace);

        public string FailureReason => "Job doesn't support being rerun";

        public async Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline?.records
                .Where(j => j.parentId == job.id)
                .OrderBy(j => j.order)
                .ToList();

            var failedTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (failedTask != null)
            {
                if (failedTask.issues?.Count > 0)
                {
                    var message = failedTask.issues[0].message;
                    if (message.Contains("The file") && message.Contains("buildinfo.json", System.StringComparison.OrdinalIgnoreCase) && message.Contains("already exists."))
                    {
                        return true;
                    }
                }

                if (job.log?.url != null)
                {
                    using (var logFile = await httpManager.GetAsync(job.log.url))
                    using (var reader = new StreamReader(logFile))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var message = line.Length > 29 ? line.Substring(29) : line;
                            if (message.StartsWith("Microsoft.VisualStudio.Services.Drop.WebApi.DropAlreadyExistsException"))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
