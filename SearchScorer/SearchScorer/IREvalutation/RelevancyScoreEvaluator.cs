using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SearchScorer.Common;

namespace SearchScorer.IREvalutation
{
    public class RelevancyScoreEvaluator
    {
        /// <summary>
        /// This is the number of results to include in the scoring. We use 5 because this is the number of results
        /// above the fold.
        /// </summary>
        private const int ResultsToEvaluate = 5;

        private readonly NormalizedDiscountedCumulativeGain _ndcg;

        public RelevancyScoreEvaluator(SearchClient searchClient)
        {
            _ndcg = new NormalizedDiscountedCumulativeGain(searchClient);
        }

        public async Task RunAsync(SearchScorerSettings settings)
        {
            var report = await GetReportAsync(settings);

            ConsoleUtility.WriteHeading("Curated Search Queries", '=');
            WriteBiggestWinnersAndLosersToConsole(report, v => v.CuratedSearchQueries);

            ConsoleUtility.WriteHeading("Feedback", '=');
            WriteBiggestWinnersAndLosersToConsole(report, v => v.FeedbackSearchQueries);
        }

        private static void WriteBiggestWinnersAndLosersToConsole<T>(
            RelevancyReport report,
            Func<VariantReport, SearchQueriesReport<T>> getReport)
        {
            Console.WriteLine($"Control:   {getReport(report.ControlReport).Score}");
            Console.WriteLine($"Treatment: {getReport(report.TreatmentReport).Score}");

            var toTreatment = getReport(report.TreatmentReport)
                .Queries
                .GroupBy(x => x.Result.Input.SearchQuery)
                .ToDictionary(x => x.Key, x => x.First().Score);
            var scoreChanges = getReport(report.ControlReport)
                .Queries
                .GroupBy(x => x.Result.Input.SearchQuery)
                .ToDictionary(x => x.Key, x => toTreatment[x.Key] - x.First().Score)
                .OrderBy(x => x.Key)
                .ToList();
            WriteSearchQueriesAndScoresToConsole(
                "Biggest Winners",
                scoreChanges.Where(x => x.Value > 0).OrderByDescending(x => x.Value).Take(20));
            WriteSearchQueriesAndScoresToConsole(
                "Biggest Losers",
                scoreChanges.Where(x => x.Value < 0).OrderBy(x => x.Value).Take(20));
        }

        private static void WriteSearchQueriesAndScoresToConsole(
            string heading,
            IEnumerable<KeyValuePair<string, double>> pairs)
        {
            var pairList = pairs.ToList();
            ConsoleUtility.WriteHeading($"{heading} ({pairList.Count})", '-');
            var longestSearchQuery = pairList.Max(x => x.Key.Length);
            foreach (var pair in pairList)
            {
                Console.WriteLine($"{pair.Key.PadRight(longestSearchQuery)} => {pair.Value:+0.0000;-0.0000;0}");
            }
        }

        private async Task<RelevancyReport> GetReportAsync(SearchScorerSettings settings)
        {
            var topQueries = TopSearchQueriesCsvReader.Read(settings.TopSearchQueriesCsvPath);
            var topSearchReferrals = GoogleAnalyticsSearchReferralsCsvReader.Read(settings.GoogleAnalyticsSearchReferralsCsvPath);

            var controlReport = await GetVariantReport(
                settings.ControlBaseUrl,
                settings,
                topQueries,
                topSearchReferrals);

            var treatmentReport = await GetVariantReport(
                settings.TreatmentBaseUrl,
                settings,
                topQueries,
                topSearchReferrals);

            return new RelevancyReport(
                controlReport,
                treatmentReport);
        }

        public async Task<VariantReport> GetCustomVariantReportAsync(
            SearchScorerSettings settings,
            string customVariantUrl)
        {
            var topQueries = TopSearchQueriesCsvReader.Read(settings.TopSearchQueriesCsvPath);
            var topSearchReferrals = GoogleAnalyticsSearchReferralsCsvReader.Read(settings.GoogleAnalyticsSearchReferralsCsvPath);

            return await GetVariantReport(
                customVariantUrl,
                settings,
                topQueries,
                topSearchReferrals);
        }

        private async Task<VariantReport> GetVariantReport(
            string baseUrl,
            SearchScorerSettings settings,
            IReadOnlyDictionary<string, int> topQueries,
            IReadOnlyDictionary<string, int> topSearchReferrals)
        {
            var curatedSearchQueriesReport = await GetCuratedSearchQueriesScoreAsync(baseUrl, settings, topQueries, topSearchReferrals);
            var feedbackSearchQueriesReport = await GetFeedbackSearchQueriesScoreAsync(baseUrl, settings);

            return new VariantReport(
                curatedSearchQueriesReport,
                feedbackSearchQueriesReport);
        }

        private async Task<SearchQueriesReport<CuratedSearchQuery>> GetCuratedSearchQueriesScoreAsync(
            string baseUrl,
            SearchScorerSettings settings,
            IReadOnlyDictionary<string, int> topQueries,
            IReadOnlyDictionary<string, int> topSearchReferrals)
        {
            var minQueryCount = topQueries.Min(x => x.Value);
            var adjustedTopQueries = topQueries.ToDictionary(
                x => x.Key,
                x =>
                {
                    if (topSearchReferrals.TryGetValue(x.Key, out var referrals))
                    {
                        return Math.Max(x.Value - referrals, minQueryCount);
                    }

                    return x.Value;
                });

            var scores = RelevancyScoreBuilder.FromCuratedSearchQueriesCsv(settings.CuratedSearchQueriesCsvPath);

            var results = await ProcessAsync(
                scores,
                baseUrl);

            return WeightByTopQueries(adjustedTopQueries, results);
        }

        private async Task<SearchQueriesReport<FeedbackSearchQuery>> GetFeedbackSearchQueriesScoreAsync(
            string baseUrl,
            SearchScorerSettings settings)
        {
            var scores = RelevancyScoreBuilder.FromFeedbackSearchQueriesCsv(settings.FeedbackSearchQueriesCsvPath);

            var results = await ProcessAsync(
                scores,
                baseUrl);

            return WeightEvently(results);
        }

        private async Task<SearchQueriesReport<SearchQueryWithSelections>> GetTopSearchSelectionsScoreAsync(
            string baseUrl,
            SearchScorerSettings settings,
            IReadOnlyDictionary<string, int> topQueries)
        {
            var topSearchSelectionScores = RelevancyScoreBuilder.FromTopSearchSelectionsCsv(settings.TopSearchSelectionsCsvPath);

            // Take the the top search selection data by query frequency.
            var selectionsOfTopQueries = topSearchSelectionScores
                .Where(x => topQueries.ContainsKey(x.SearchQuery))
                .OrderByDescending(x => topQueries[x.SearchQuery])
                .Take(1000);

            var results = await ProcessAsync(
                selectionsOfTopQueries,
                baseUrl);

            return WeightByTopQueries(topQueries, results);
        }

        private static SearchQueriesReport<T> WeightEvently<T>(ConcurrentBag<RelevancyScoreResult<T>> results)
        {
            var totalCount = 1.0 * results.Count;

            var weightedResults = results
                .Select(x => new WeightedRelevancyScoreResult<T>(
                    x,
                    x.ResultScore / totalCount))
                .ToList();

            return new SearchQueriesReport<T>(weightedResults);
        }

        private static SearchQueriesReport<T> WeightByTopQueries<T>(
            IReadOnlyDictionary<string, int> topQueries,
            ConcurrentBag<RelevancyScoreResult<T>> results)
        {
            // Weight the queries that came from top search selections by their query count.
            var totalQueryCount = 0;
            var resultsAndWeights = new List<KeyValuePair<RelevancyScoreResult<T>, int>>();
            foreach (var result in results)
            {
                var queryCount = topQueries[result.Input.SearchQuery];
                resultsAndWeights.Add(new KeyValuePair<RelevancyScoreResult<T>, int>(result, queryCount));
                totalQueryCount += queryCount;
            }

            var weightedResults = new List<WeightedRelevancyScoreResult<T>>();
            foreach (var pair in resultsAndWeights)
            {
                weightedResults.Add(new WeightedRelevancyScoreResult<T>(
                    pair.Key,
                    1.0 * pair.Value / totalQueryCount));
            }

            return new SearchQueriesReport<T>(weightedResults);
        }

        private async Task<ConcurrentBag<RelevancyScoreResult<T>>> ProcessAsync<T>(
            IEnumerable<SearchQueryRelevancyScores<T>> queries,
            string baseUrl)
        {
            var work = new ConcurrentBag<SearchQueryRelevancyScores<T>>(queries);
            var output = new ConcurrentBag<RelevancyScoreResult<T>>();

            var workers = Enumerable
                .Range(0, 16)
                .Select(async x =>
                {
                    await Task.Yield();

                    while (work.TryTake(out var query))
                    {
                        try
                        {
                            var result = await _ndcg.ScoreAsync(query, baseUrl, ResultsToEvaluate);
                            Console.WriteLine($"[{baseUrl}] {query.SearchQuery} => {result.ResultScore}");
                            output.Add(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{baseUrl}] {query.SearchQuery} => {ex}");
                            throw;
                        }
                    }
                })
                .ToList();

            await Task.WhenAll(workers);

            return output;
        }
    }
}
