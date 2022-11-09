using Newtonsoft.Json;
using System.Collections.Generic;

namespace ci_testfailure_analyzer.Models.AzDO
{
    public class TestItem
    {
        public int Id { get; set; }
        public int RunId { get; set; }
        public int refId { get; set; }
        public string outcome { get; set; }
        public int priority { get; set; }
        public string automatedTestName { get; set; }
        public string automatedTestStorage { get; set; }
        public string owner { get; set; }
        public string testCaseTitle { get; set; }
        public float durationInMs { get; set; }
    }

    public class TestRes
    {
        [JsonProperty("Value")]
        public List<TestItem> TestItems { get; set; }
        public int count { get; set; }
    }
}
