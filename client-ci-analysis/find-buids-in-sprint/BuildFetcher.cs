using find_buids_in_sprint.FailureDetectors;
using find_buids_in_sprint.Models.AzDO;
using find_buids_in_sprint.Models.ClientCiAnalysis;
using NetworkManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace find_buids_in_sprint
{
    internal class BuildFetcher
    {
        private readonly HttpManager _httpManager;

        private readonly IReadOnlyList<IFailureDetector> _failureDetectors;

        public BuildFetcher(HttpManager httpManager)
        {
            _httpManager = httpManager;

            var failureDetectorType = typeof(IFailureDetector);

            _failureDetectors = failureDetectorType.Assembly
                .GetTypes()
                .Where(t => t.IsAssignableTo(failureDetectorType) && t != failureDetectorType)
                .Select(t=> Activator.CreateInstance(t))
                .Cast<IFailureDetector>()
                .ToList();
        }

        public async Task<Build> GetBuildAsync(BuildInfo ciBuild)
        {
            try
            {
                var analysisBuild = new Build()
                {
                    id = ciBuild.id,
                    buildNumber = ciBuild.buildNumber,
                    url = ciBuild.links["web"].href,
                    result = ciBuild.result,
                    finishTime = ciBuild.finishTime,
                    jobs = new Dictionary<string, List<Attempt>>()
                };

                if (!ciBuild.validationResults.Any(r => string.Equals("error", r.result, StringComparison.OrdinalIgnoreCase)))
                {
                    Timeline[] timelines = await GetTimelinesAsync(ciBuild);

                    for (int i = 0; i < timelines.Length; i++)
                    {
                        var attemptNumber = i + 1;
                        foreach (var job in timelines[i].records.Where(r => r.attempt == attemptNumber && string.Equals("job", r.type, StringComparison.OrdinalIgnoreCase)))
                        {
                            var jobName = job.name;

                            if (!analysisBuild.jobs.TryGetValue(jobName, out List<Attempt> attempts))
                            {
                                attempts = new List<Attempt>();
                                analysisBuild.jobs.Add(jobName, attempts);
                            }

                            var attempt = new Attempt()
                            {
                                result = job.result,
                                issue = await GetIssueReasonAsync(ciBuild, job, timelines[i])
                            };

                            if (job.startTime != null && job.finishTime != null)
                            {
                                attempt.duration = (job.finishTime.Value - job.startTime.Value).ToString("g");
                            }

                            attempts.Add(attempt);
                        }
                    }
                }

                return analysisBuild;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<Timeline[]> GetTimelinesAsync(BuildInfo ciBuild)
        {
            var timelineUrl = ciBuild.links["timeline"].href;
            Timeline timeline;
            using (var responseStream = await _httpManager.GetAsync(timelineUrl))
            {
                timeline = await System.Text.Json.JsonSerializer.DeserializeAsync<Timeline>(responseStream);
            }

            var stage = timeline.records.Single(r => r.parentId == null);
            var timelines = new Timeline[stage.attempt];
            timelines[stage.attempt - 1] = timeline;

            while (stage.attempt > 1)
            {
                var prevTimelineId = stage.previousAttempts.Where(a => a.attempt == stage.attempt - 1).Single().timelineId;
                timelineUrl = ciBuild.links["timeline"].href + "/" + prevTimelineId;
                using (var responseStream = await _httpManager.GetAsync(timelineUrl))
                {
                    timeline = await System.Text.Json.JsonSerializer.DeserializeAsync<Timeline>(responseStream);
                }
                stage = timeline.records.Single(r => r.parentId == null);
                timelines[stage.attempt - 1] = timeline;
            }

            return timelines;
        }

        private async Task<string> GetIssueReasonAsync(BuildInfo build, TimelineRecord job, Timeline timeline)
        {
            if (job.result == "succeeded" || job.result == "skipped")
            {
                return null;
            }

            foreach (var detector in _failureDetectors)
            {
                if (await detector.FailureDetectedAsync(build, job, timeline, _httpManager))
                {
                    return detector.FailureReason;
                }
            }

            return null;
        }
    }
}
