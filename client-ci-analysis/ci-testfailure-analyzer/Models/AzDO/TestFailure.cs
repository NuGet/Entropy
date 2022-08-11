using Newtonsoft.Json;
using System.Collections.Generic;

namespace ci_testfailure_analyzer.Models.AzDO
{
    public class TestFailure
    {
        public DataProviders dataProviders { get; set; }
    }

    public class _2
    {
        public int outcome { get; set; }
        public int count { get; set; }
        public string duration { get; set; }
    }

    public class _3
    {
        public int totalTestCount { get; set; }
        public int notReportedTestCount { get; set; }
        public string duration { get; set; }
        public AggregatedResultDetailsByOutcome aggregatedResultDetailsByOutcome { get; set; }
        public int outcome { get; set; }
        public int count { get; set; }
    }

    public class AggregatedResultDetailsByOutcome
    {
        public _2 _2 { get; set; }
        public _3 _3 { get; set; }
    }

    public class CurrentContext
    {
        public int pipelineId { get; set; }
    }

    public class DataProviders
    {
        [JsonProperty("ms.vss-test-web.test-tab-unifiedPipeline-summary-data-provider")]
        public MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider { get; set; }
    }


    public class ExistingFailures
    {
        public int count { get; set; }
        public List<TestResult> testResults { get; set; }
    }

    public class FixedTests
    {
        public int count { get; set; }
        public List<object> testResults { get; set; }
    }

    public class MsVssTestWebTestTabUnifiedPipelineSummaryDataProvider
    {
        public CurrentContext currentContext { get; set; }
        public ResultSummary resultSummary { get; set; }
        public ResultsAnalysis resultsAnalysis { get; set; }
        public RunSummary runSummary { get; set; }
    }

    public class NewFailures
    {
        public int count { get; set; }
        public List<TestResult> testResults { get; set; }
    }

    public class PreviousContext
    {
        public int pipelineId { get; set; }
    }

    public class ResultsAnalysis
    {
        public PreviousContext previousContext { get; set; }
        public TestFailuresAnalysis testFailuresAnalysis { get; set; }
        public ResultsDifference resultsDifference { get; set; }
    }

    public class ResultsDifference
    {
        public int increaseInTotalTests { get; set; }
        public int increaseInFailures { get; set; }
        public int increaseInPassedTests { get; set; }
        public int increaseInNonImpactedTests { get; set; }
        public int increaseInOtherTests { get; set; }
        public string increaseInDuration { get; set; }
    }

    public class ResultSummary
    {
        public ResultSummaryByRunState resultSummaryByRunState { get; set; }
    }

    public class ResultSummaryByRunState
    {
        public _3 _3 { get; set; }
    }

    public class RunSummary
    {
        public int totalRunsCount { get; set; }
        public string duration { get; set; }
        public RunSummaryByState runSummaryByState { get; set; }
        public RunSummaryByOutcome runSummaryByOutcome { get; set; }
    }

    public class RunSummaryByOutcome
    {
        public int _0 { get; set; }
        public int _1 { get; set; }
    }

    public class RunSummaryByState
    {
        public int _3 { get; set; }
    }

    public class TestFailuresAnalysis
    {
        public NewFailures newFailures { get; set; }
        public ExistingFailures existingFailures { get; set; }
        public FixedTests fixedTests { get; set; }
    }

    public class TestResult
    {
        public int testResultId { get; set; }
        public int testRunId { get; set; }
    }
}
