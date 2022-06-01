using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#nullable disable

namespace GithubIssueTagger
{
    public static partial class PullRequestUtilities
    {
        // Ugly code alert. This method is a collection of random code :) 
        public static async Task ProcessPullRequestStatsAsync(GitHubClient client)
        {
            bool FetchDataFromRemote = false;
            bool Preprocess = false;
            bool AnalyzeReviewDistribution = false;
            bool AnalyzeTimeToReview = true;
            var outputFileName = "statistics.json";
            var closedIssueFileName = "closed-processed.json";

            if (FetchDataFromRemote)
            {
                // Load code paths
                var statistics = await PullRequestUtilities.GeneratePullRequestStatistics(client, "nuget", "NuGet.Client", 3200, 3302);
                var json = JsonConvert.SerializeObject(statistics, Formatting.Indented);
                File.WriteAllText(outputFileName, json);
            }

            if (Preprocess)
            {
                var lines = File.ReadAllText(outputFileName);
                var stats = JsonConvert.DeserializeObject<IDictionary<int, PullRequestStatistic>>(lines);
                var importantStat = stats.Where(e => e.Value.State == "closed" && e.Value.MergedDate != null).OrderByDescending(e => e.Value.MergedDate)
                    .Select(e => new Stat(e.Value.Creator, e.Key, e.Value.CreatedAt, (DateTimeOffset)e.Value.MergedDate, (DateTimeOffset)e.Value.GetFirstEngagementDate(), e.Value.GetReviewers())).ToArray();
                File.WriteAllText(closedIssueFileName, JsonConvert.SerializeObject(importantStat, Formatting.Indented));
            }

            if (AnalyzeReviewDistribution)
            {
                var lines = File.ReadAllText(closedIssueFileName);
                var stats = JsonConvert.DeserializeObject<Stat[]>(lines);
                var interestingPeople = new string[] { "kartheekp-ms", "cristinamanum", "zivkan", "heng-liu", "nkolev92", "aortiz-msft", "dominoFire", "rrelyea", "dtivel", "donnie-msft", "zkat" };

                var reviews = interestingPeople.ToDictionary(item => item, item => 0);
                var potentialReviews = interestingPeople.ToDictionary(item => item, item => stats.Length);

                foreach (var issue in stats)
                {
                    if (potentialReviews.ContainsKey(issue.Name))
                    {
                        potentialReviews[issue.Name]--;
                    }

                    foreach (var reviewer in issue.Reviewers)
                    {
                        if (reviews.ContainsKey(reviewer))
                        {
                            reviews[reviewer]++;
                        }
                    }
                }

                var finalDict = new Dictionary<string, Tuple<int, int>>();

                foreach (var review in reviews)
                {
                    var countReview = review.Value;
                    var countPotentialReview = potentialReviews[review.Key];

                    finalDict.Add(review.Key, new Tuple<int, int>(countReview, countPotentialReview));
                }

                foreach (var entry in finalDict)
                {
                    Console.WriteLine($"{entry.Key},{entry.Value.Item1},{entry.Value.Item2}");
                }
            }

            if (AnalyzeTimeToReview)
            {
                var lines = File.ReadAllText(closedIssueFileName);
                var stats = JsonConvert.DeserializeObject<Stat[]>(lines);
                var timings = new List<Tuple<int, TimeSpan, TimeSpan>>();

                foreach (var stat in stats)
                {
                    timings.Add(new Tuple<int, TimeSpan, TimeSpan>(stat.Number, stat.FirstReview.Subtract(stat.StartedOn), stat.EndedOn.Subtract(stat.StartedOn)));
                }

                foreach (var entry in timings)
                {
                    Console.WriteLine($"{entry.Item1},{entry.Item2.TotalDays},{entry.Item3.TotalDays}{Environment.NewLine}");
                }
            }
        }

        public class Stat
        {
            public string Name { get; }
            public int Number { get; }
            public DateTimeOffset StartedOn { get; }
            public DateTimeOffset EndedOn { get; }
            public DateTimeOffset FirstReview { get; }
            public IList<string> Reviewers { get; }

            public Stat(string name, int number, DateTimeOffset startedOn, DateTimeOffset endedOn, DateTimeOffset firstReview, IList<string> reviewers)
            {
                Name = name;
                Number = number;
                StartedOn = startedOn;
                EndedOn = endedOn;
                FirstReview = firstReview;
                Reviewers = reviewers;
            }
        }

        public static async Task<IDictionary<int, PullRequestStatistic>> GeneratePullRequestStatistics(GitHubClient client, string org, string repo, int from, int to)
        {
            var statistics = new Dictionary<int, PullRequestStatistic>();

            for (var pullNumber = from; pullNumber <= to; pullNumber++)
            {
                try
                {
                    var pullRequest = await client.PullRequest.Get(org, repo, pullNumber);
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
