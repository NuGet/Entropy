namespace SearchScorer
{
    public class SearchScorerSettings
    {
        public string ControlBaseUrl { get; set; }
        public string TreatmentBaseUrl { get; set; }
        public string FeedbackSearchQueriesCsvPath { get; set; }
        public string CuratedSearchQueriesCsvPath { get; set; }
        public string ClientCuratedSearchQueriesCsvPath { get; set; }
        public string TopSearchQueriesCsvPath { get; set; }
        public string TopSearchSelectionsCsvPath { get; set; }
        public string TopSearchSelectionsV2CsvPath { get; set; }
        public string GoogleAnalyticsSearchReferralsCsvPath { get; set; }
        public string GitHubUsageJsonPath { get; set; }
        public string GitHubUsageCsvPath { get; set; }
        public string ProbeResultsCsvPath { get; set; }

        // The following settings are only necessary for the "probe" command
        public string AzureSearchServiceName { get; set; }
        public string AzureSearchIndexName { get; set; }
        public string AzureSearchApiKey { get; set; }
    }
}
