using find_buids_in_sprint.Models.AzDO;
using NetworkManager;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace find_buids_in_sprint.FailureDetectors
{
    internal class TestFailureDetector : IFailureDetector
    {
        private static readonly Regex assemblyTestSummaryLine = new Regex(@"^\s*(?<assembly>[\w\.]*)\s*Total:\s*(?<total>\d+), Errors:\s*(?<errors>\d+), Failed:\s*(?<failed>\d+), Skipped:\s*(?<skipped>\d+), Time:\s*(?<time>\d+\.\d+)s$");

        public string FailureReason => "Test failure";

        public async Task<bool> FailureDetectedAsync(BuildInfo build, TimelineRecord job, Timeline timeline, HttpManager httpManager)
        {
            var tasks = timeline.records
                .Where(r => r.parentId == job.id)
                .OrderBy(r => r.order)
                .ToList();

            var probableTask = tasks.FirstOrDefault(t => t.result == job.result);

            if (probableTask?.log == null)
            {
                return false;
            }

            using (var fileStream = await httpManager.GetAsync(probableTask.log.url))
            {
                fileStream.Position = 0;

                using (var textStream = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = textStream.ReadLine()) != null)
                    {
                        // skip timestamp
                        var message =
                            line.Length > 29
                            ? line.Substring(29)
                            : line;

                        if (message == "Mono tests failed!")
                        {
                            return true;
                        }

                        var match = assemblyTestSummaryLine.Match(message);
                        if (match.Success)
                        {
                            var errors = int.Parse(match.Groups["errors"].Value);
                            var failed = int.Parse(match.Groups["failed"].Value);
                            if (errors > 0 || failed > 0)
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
