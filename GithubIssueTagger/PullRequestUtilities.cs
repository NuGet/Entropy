using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GithubIssueTagger
{
    public static partial class PullRequestUtilities
    {
        public static string[] ActiveTeamMembers =
            new string[] {
                "aortiz-msft",
                "donnie-msft",
                "jebriede",
                "kartheekp-ms",
                "martinrrm",
                "nkolev92",
                "zivkan",
                "jeffkl",
                "Nigusu-Allehu",
            };

        /// <summary>
        /// Get some pull request stats printed on the console. This method only analyzes closed PRs.
        /// </summary>
        /// <param name="client">GH Client</param>
        /// <param name="fetchDataFromRemote">Whether to fetch new data from remote. Usually you'd want to do this once per PR range. Optimization for local running.</param>
        /// <param name="preprocess">>Whether to process new data in the file fetched remotely. Usually you'd want to do this once per PR range. Optimization for local running.</param>
        /// <param name="analyzeReviewDistribution">Print data about who reviewed how many PRs.</param>
        /// <param name="analyzeTimeToReview">Whether to print data about how long individual PRs took to the review</param>
        /// <param name="oldestPR">PR id to start with.</param>
        /// <param name="newestPR">PR id to end with.</param>
        /// <returns></returns>
        public static async Task ProcessPullRequestStatsAsync(GitHubClient client, bool fetchDataFromRemote, bool preprocess, bool analyzeReviewDistribution, bool analyzeTimeToReview, int oldestPR, int newestPR)
        {
            var rawPullRequestDataFileName = "statistics.json";
            var processedPullRequestFileName = "closed-processed.json";

            if (fetchDataFromRemote)
            {
                // Load code paths
                var statistics = await GeneratePullRequestStatistics(client, "NuGet", "NuGet.Client", oldestPR, newestPR);
                var json = JsonConvert.SerializeObject(statistics, Formatting.Indented);
                File.WriteAllText(rawPullRequestDataFileName, json);
            }

            if (preprocess)
            {
                IDictionary<int, PullRequestStatistic> PRStatistics = JsonConvert.DeserializeObject<IDictionary<int, PullRequestStatistic>>(File.ReadAllText(rawPullRequestDataFileName))!;
                var mergedPRs = PRStatistics.Where(e => e.Value.State == "closed" && e.Value.MergedDate != null)
                    .OrderByDescending(e => e.Value.MergedDate);
                List<PullRequestReviewData> pullRequestReviewData = new();
                foreach (var mergedPR in mergedPRs)
                {
                    var statistic = mergedPR.Value;
                    pullRequestReviewData.Add(new
                        PullRequestReviewData(statistic.Creator,
                            mergedPR.Key,
                            statistic.CreatedAt,
                            (DateTimeOffset)statistic.MergedDate!,
                            (DateTimeOffset)statistic.GetFirstEngagementDate(),
                            statistic.GetReviewers()));
                }

                File.WriteAllText(processedPullRequestFileName, JsonConvert.SerializeObject(pullRequestReviewData, Formatting.Indented));
            }

            if (analyzeReviewDistribution)
            {
                Dictionary<string, Tuple<int, int>> reviewDistributionData = GetReviewDistribution(processedPullRequestFileName);

                Console.WriteLine("User,PRsReviewed,MaxReviewablePRs");
                foreach (var entry in reviewDistributionData)
                {
                    Console.WriteLine($"{entry.Key},{entry.Value.Item1},{entry.Value.Item2}");
                }
            }

            if (analyzeTimeToReview)
            {
                List<Tuple<int, TimeSpan, TimeSpan>> timings = GetTimeToReview(processedPullRequestFileName);

                Console.WriteLine("PRId,TimeToFirstReview,TimeToMerge");
                foreach (var entry in timings)
                {
                    Console.WriteLine($"{entry.Item1},{entry.Item2.TotalDays},{entry.Item3.TotalDays}");
                }
            }
        }

        private static List<Tuple<int, TimeSpan, TimeSpan>> GetTimeToReview(string processedPullRequestFileName)
        {
            PullRequestReviewData[] pullRequestReviewData = JsonConvert.DeserializeObject<PullRequestReviewData[]>(File.ReadAllText(processedPullRequestFileName))!;
            var timings = new List<Tuple<int, TimeSpan, TimeSpan>>();

            foreach (var entry in pullRequestReviewData)
            {
                timings.Add(new Tuple<int, TimeSpan, TimeSpan>(entry.Number,
                    entry.FirstReview.Subtract(entry.StartedOn),
                    entry.EndedOn.Subtract(entry.StartedOn)));
            }
            return timings;
        }

        private static Dictionary<string, Tuple<int, int>> GetReviewDistribution(string processedPullRequestFileName)
        {
            PullRequestReviewData[] pullRequestReviewData = JsonConvert.DeserializeObject<PullRequestReviewData[]>(File.ReadAllText(processedPullRequestFileName))!;
            int maxReviews = pullRequestReviewData.Length;
            var reviews = ActiveTeamMembers.ToDictionary(item => item, item => 0);
            var potentialReviews = ActiveTeamMembers.ToDictionary(item => item, item => maxReviews);

            foreach (var entry in pullRequestReviewData)
            {
                if (potentialReviews.ContainsKey(entry.Name))
                {
                    potentialReviews[entry.Name]--;
                }

                foreach (var reviewer in entry.Reviewers)
                {
                    if (reviews.ContainsKey(reviewer))
                    {
                        reviews[reviewer]++;
                    }
                }
            }

            var reviewDistributionData = new Dictionary<string, Tuple<int, int>>();

            foreach (var review in reviews)
            {
                var countReview = review.Value;
                var countPotentialReview = potentialReviews[review.Key];

                reviewDistributionData.Add(review.Key, new Tuple<int, int>(countReview, countPotentialReview));
            }

            return reviewDistributionData;
        }

        private static async Task<IDictionary<int, PullRequestStatistic>> GeneratePullRequestStatistics(GitHubClient client, string org, string repo, int from, int to)
        {
            var statistics = new Dictionary<int, PullRequestStatistic>();

            for (var pullNumber = from; pullNumber <= to; pullNumber++)
            {
                try
                {
                    var pullRequest = await client.PullRequest.Get(org, repo, pullNumber);

                    if(pullRequest.State == ItemState.Open)
                    {
                        // We are only interested in closed PRs for now.
                        continue;
                    }

                    var commentsForPullRequest = await client.PullRequest.ReviewComment.GetAll(org, repo, pullNumber);
                    var reviewsForPullRequest = await client.PullRequest.Review.GetAll(org, repo, pullNumber);

                    var reviews = new List<ReviewStatistic>();

                    foreach (var comment in commentsForPullRequest)
                    {
                        reviews.Add(new ReviewStatistic(comment.User.Login, comment.CreatedAt, false));
                    }

                    foreach (var review in reviewsForPullRequest)
                    {
                        reviews.Add(new ReviewStatistic(review.User.Login, review.SubmittedAt, true));
                    }
                    var pullRequestStat = new PullRequestStatistic(pullNumber, pullRequest.User.Login, pullRequest.CreatedAt, pullRequest.State.StringValue, pullRequest.MergedAt, reviews);

                    statistics.Add(pullNumber, pullRequestStat);
                }
                catch (Exception e)
                {
                    Console.WriteLine(pullNumber + e.ToString());
                }
            }
            return statistics;
        }
    }
}
