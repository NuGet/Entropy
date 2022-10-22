using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ci_testfailure_analyzer.Models.AzDO
{
    public class TestResults
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }
        [JsonProperty("fields")]
        public List<string> Fields { get; set; }
    }

    public class Result
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("project")]
        public Project Project { get; set; }
        [JsonProperty("startedDate")]
        public DateTime StartedDate { get; set; }
        [JsonProperty("completedDate")]
        public DateTime CompletedDate { get; set; }
        [JsonProperty("outcome")]
        public string Outcome { get; set; }
        [JsonProperty("testCase")]
        public TestCase TestCase { get; set; }
        [JsonProperty("testPoint")]
        public TestPoint TestPoint { get; set; }
        [JsonProperty("testRun")]
        public TestRun TestRun { get; set; }
        [JsonProperty("priority")]
        public int Priority { get; set; }
        [JsonProperty("failureType")]
        public string FailureType { get; set; }
        [JsonProperty("automatedTestStorage")]
        public string AutomatedTestStorage { get; set; }
        [JsonProperty("testCaseTitle")]
        public string TestCaseTitle { get; set; }
        [JsonProperty("customFields")]
        public List<object> CustomFields { get; set; }
        [JsonProperty("failingSince")]
        public FailingSince FailingSince { get; set; }
        [JsonProperty("testCaseReferenceId")]
        public int TestCaseReferenceId { get; set; }
        [JsonProperty("automatedTestName")]
        public string AutomatedTestName { get; set; }
    }
    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
    public class TestCase
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    public class TestPoint
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
    public class TestRun
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    public class FailingSince
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("build")]
        public Build Build { get; set; }
    }
    public class Build
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("definitionId")]
        public int DefinitionId { get; set; }
        [JsonProperty("number")]
        public string Number { get; set; }
        [JsonProperty("buildSystem")]
        public string BuildSystem { get; set; }
    }

}
