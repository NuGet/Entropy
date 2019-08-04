using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SearchScorer.Common;

namespace SearchScorer.Feedback
{
    public static class FeedbackEvaluator
    {
        private const int FoldMaxIndex = 4;
        private const int PageSize = 1000;
        private const int Take = PageSize;

        private static readonly int MaxResultIndexBucketLength = Enum.GetNames(typeof(ResultIndexBucket)).Max(x => x.Length);
        private static readonly int MaxFeedbackResultTypeLength = Enum.GetNames(typeof(FeedbackResultType)).Max(x => x.Length);

        public static async Task RunAsync(HttpClient httpClient, SearchScorerSettings settings)
        {
            Console.WriteLine("Starting feedback evaluation.");
            Console.WriteLine($"Control base URL:   {settings.ControlBaseUrl}");
            Console.WriteLine($"Treatment base URL: {settings.TreatmentBaseUrl}");
            Console.WriteLine();

            var searchClient = new SearchClient(httpClient);

            var testSearchQueries = TestSearchQueryCsvReader.Read(settings.TestSearchQueryCsvPath);
            var feedbackItems = testSearchQueries
                .Select(x => new FeedbackItem(
                    x.Source,
                    x.FeedbackDisposition,
                    x.SearchQuery,
                    x.MostRelevantPackageIds,
                    x.Buckets.ToArray()));

            var input = new ConcurrentBag<FeedbackItem>(feedbackItems);
            var output = new ConcurrentBag<FeedbackResult>();
            var processTasks = Enumerable
                .Range(0, 16)
                .Select(x => ProcessFeedbackAsync(
                    searchClient,
                    settings.ControlBaseUrl,
                    settings.TreatmentBaseUrl,
                    input,
                    output))
                .ToList();

            var cts = new CancellationTokenSource();
            var outputTask = OutputFeedbackResultsAsync(output, cts.Token);

            await Task.WhenAll(processTasks);
            cts.Cancel();
            await outputTask;
        }

        private static async Task OutputFeedbackResultsAsync(
            ConcurrentBag<FeedbackResult> output,
            CancellationToken token)
        {
            await Task.Yield();

            var aggregator = new FeedbackResultAggregator();

            using (var csvWriter = new FeedbackCsvWriter())
            {
                while (true)
                {
                    if (!output.TryTake(out var result))
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                        continue;
                    }

                    WriteResultToConsole(result);
                    csvWriter.WriteResult(result);
                    aggregator.Add(result);
                }
            }

            WriteSummaryToConsole(aggregator);
        }

        private static void WriteSummaryToConsole(FeedbackResultAggregator aggregator)
        {
            ConsoleUtility.WriteHeading("Feedback Result Summary", '=');

            var resultTypeToResults = aggregator.GetResultTypeToResults();
            var totalCount = resultTypeToResults.Sum(p => p.Value.Count);
            Console.WriteLine();
            Console.WriteLine("Result type counts:");
            Console.WriteLine($"  {$"Total:".PadRight(MaxFeedbackResultTypeLength + 1)} {totalCount}");
            foreach (var pair in resultTypeToResults)
            {
                Console.WriteLine($"  {$"{pair.Key}:".PadRight(MaxFeedbackResultTypeLength + 1)} {pair.Value.Count}");
            }

            Console.WriteLine();
            var bothFixedCount = resultTypeToResults[FeedbackResultType.BothFixed].Count;
            var bothFixedAndNewIsBetter = resultTypeToResults[FeedbackResultType.BothFixed]
                .Where(x => x.FeedbackItem.Disposition == FeedbackDisposition.NewIsGreat)
                .Count();
            var fixedCount = resultTypeToResults[FeedbackResultType.Fixed].Count;
            Console.WriteLine(
                $"Percent fixed (including BothFixed):              " +
                $"{Math.Round(100.0 * (fixedCount + bothFixedCount) / totalCount, 2)}%");
            Console.WriteLine(
                $"Percent fixed (excluding BothFixed):              " +
                $"{Math.Round(100.0 * fixedCount / (totalCount - bothFixedCount), 2)}%");
            Console.WriteLine(
                $"Percent fixed (including BothFixed & NewIsGreat): " +
                $"{Math.Round(100.0 * (fixedCount + bothFixedAndNewIsBetter) / ((totalCount - bothFixedCount) + bothFixedAndNewIsBetter), 2)}%");

            var maxQueryLength = aggregator.GetMaxQueryLength();

            ConsoleUtility.WriteHeading("Significant Regressions", '-');
            WriteQueriesToConsole(maxQueryLength, "that dropped off the first page", aggregator.GetResultsThatDroppedOffTheFirstPage());
            WriteQueriesToConsole(maxQueryLength, "that dropped below the fold", aggregator.GetResultsThatDroppedBelowTheFold());

            ConsoleUtility.WriteHeading("Minor Regressions", '-');
            WriteQueriesToConsole(maxQueryLength, "that moved down, above the fold", aggregator.GetResultsThatMovedDownAboveTheFold());
            WriteQueriesToConsole(maxQueryLength, "that moved down, below the fold", aggregator.GetResultsThatMovedDownBelowTheFold());

            ConsoleUtility.WriteHeading("Minor Improvements", '-');
            WriteQueriesToConsole(maxQueryLength, "that rose to below the fold", aggregator.GetResultsThatRoseToBelowTheFold());
            WriteQueriesToConsole(maxQueryLength, "that moved up, above the fold", aggregator.GetResultsThatMovedUpAboveTheFold());
            WriteQueriesToConsole(maxQueryLength, "that moved up, below the fold", aggregator.GetResultsThatMovedUpBelowTheFold());

            ConsoleUtility.WriteHeading("All Feedback By Result Type", '-');
            WriteQueriesToConsole(maxQueryLength, "of result type BothBroken", aggregator.GetResultTypeToResults()[FeedbackResultType.BothBroken]);
            WriteQueriesToConsole(maxQueryLength, "of result type Regressed", aggregator.GetResultTypeToResults()[FeedbackResultType.Regressed]);
            WriteQueriesToConsole(maxQueryLength, "of result type Fixed", aggregator.GetResultTypeToResults()[FeedbackResultType.Fixed]);
            WriteQueriesToConsole(maxQueryLength, "of result type BothFixed", aggregator.GetResultTypeToResults()[FeedbackResultType.BothFixed]);
        }

        private static void WriteQueriesToConsole(int maxQueryLength, string label, List<FeedbackResult> results)
        {
            Console.WriteLine();
            Console.WriteLine($"Feedback {label} ({results.Count}):");
            foreach (var result in results)
            {
                Console.WriteLine($"  {result.FeedbackItem.Query.PadRight(maxQueryLength)} => {string.Join(" | ", result.FeedbackItem.MostRelevantPackageIds)}");
            }

            if (results.Count == 0)
            {
                Console.WriteLine("  (none)");
            }
        }

        private static void WriteResultToConsole(FeedbackResult result)
        {
            Console.WriteLine($"{result.FeedbackItem.Query} => {string.Join(" | ", result.FeedbackItem.MostRelevantPackageIds)}");

            ConsoleColor color;
            switch (result.Type)
            {
                case FeedbackResultType.Regressed:
                    color = ConsoleColor.Red;
                    break;
                case FeedbackResultType.Fixed:
                    color = ConsoleColor.Green;
                    break;
                case FeedbackResultType.BothBroken:
                    color = ConsoleColor.Yellow;
                    break;
                case FeedbackResultType.BothFixed:
                    color = ConsoleColor.DarkGray;
                    break;
                default:
                    throw new NotImplementedException();
            }

            Console.Write("[ ");
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(PadBoth(result.Type.ToString(), MaxFeedbackResultTypeLength));
            Console.ForegroundColor = currentColor;
            Console.WriteLine(
                $" ] " +
                $"{DisplayResultIndexBucket(result.ControlResult.ResultIndexBucket)} -> {DisplayResultIndexBucket(result.TreatmentResult.ResultIndexBucket)} " +
                $"( {DisplayResultIndex(result.ControlResult.ResultIndex)} -> {DisplayResultIndex(result.TreatmentResult.ResultIndex)} )");
            Console.WriteLine();
        }

        private static string DisplayResultIndexBucket(ResultIndexBucket resultIndexBucket)
        {
            return PadBoth(resultIndexBucket.ToString(), MaxResultIndexBucketLength);
        }

        private static string DisplayResultIndex(int? result)
        {
            return PadBoth(result == null ? "?" : result.ToString(), 3);
        }

        /// <summary>
        /// Source: https://stackoverflow.com/a/17590723
        /// </summary>
        private static string PadBoth(string source, int length)
        {
            int spaces = length - source.Length;
            int padLeft = spaces / 2 + source.Length;
            return source.PadLeft(padLeft).PadRight(length);
        }

        private static async Task ProcessFeedbackAsync(
            SearchClient searchClient,
            string controlBaseUrl,
            string treatmentBaseUrl,
            ConcurrentBag<FeedbackItem> input,
            ConcurrentBag<FeedbackResult> output)
        {
            await Task.Yield();

            while (input.TryTake(out var feedbackItem))
            {
                var result = await GetFeedbackResultAsync(
                    searchClient,
                    controlBaseUrl,
                    treatmentBaseUrl,
                    feedbackItem);

                output.Add(result);
            }
        }

        private static async Task<FeedbackResult> GetFeedbackResultAsync(
            SearchClient searchClient,
            string controlBaseUrl,
            string treatmentBaseUrl,
            FeedbackItem feedback)
        {
            var controlTask = searchClient.SearchAsync(controlBaseUrl, feedback.Query, Take);
            var treatmentTask = searchClient.SearchAsync(treatmentBaseUrl, feedback.Query, Take);

            await Task.WhenAll(controlTask, treatmentTask);

            var controlResult = GetVariantResult(
                feedback,
                controlTask.Result);

            var treatmentResult = GetVariantResult(
                feedback,
                treatmentTask.Result);

            var type = FeedbackResultType.BothBroken;

            if (controlResult.ResultIndexBucket == ResultIndexBucket.AboveFold)
            {
                type |= FeedbackResultType.Regressed;
            }

            if (treatmentResult.ResultIndexBucket == ResultIndexBucket.AboveFold)
            {
                type |= FeedbackResultType.Fixed;
            }

            return new FeedbackResult(
                feedback,
                type,
                controlResult,
                treatmentResult);
        }

        static VariantResult GetVariantResult(
            FeedbackItem feedbackItem,
            SearchResponse searchResponse)
        {
            var results = searchResponse
                .Data
                .Select((x, i) => new { x.Id, Index = i })
                .ToList();

            // Find the best match given in the most relevant package IDs.
            int? best = null;
            var foundIds = new List<string>();

            foreach (var packageIdPattern in feedbackItem.MostRelevantPackageIds)
            {
                var regex = WildcardUtility.GetPackageIdWildcareRegex(packageIdPattern);
                foreach (var result in results)
                {
                    if (regex.IsMatch(result.Id))
                    {
                        foundIds.Add(result.Id);

                        if (best == null || result.Index < best)
                        {
                            best = result.Index;
                        }
                    }
                }
            }

            // Determine the page index and if the result index is above the fold (indicating success).
            int? resultIndex = null;
            int? pageIndex = null;
            if (foundIds.Any())
            {
                resultIndex = best;
                pageIndex = resultIndex.Value / PageSize;
            }

            ResultIndexBucket resultIndexBucket;
            if (resultIndex != null && resultIndex.Value <= FoldMaxIndex)
            {
                resultIndexBucket = ResultIndexBucket.AboveFold;
            }
            else if (pageIndex == 0)
            {
                resultIndexBucket = ResultIndexBucket.BelowFold;
            }
            else
            {
                resultIndexBucket = ResultIndexBucket.NotInFirstPage;
            }

            return new VariantResult(
                resultIndex,
                resultIndexBucket,
                pageIndex,
                foundIds,
                searchResponse);
        }
    }
}
