using Newtonsoft.Json;
using System.Collections.Generic;

namespace ci_testfailure_analyzer.Models.AzDO
{
    internal class TestFlakinessStatus
    {
        [JsonProperty("customFields")]
        public List<customField> CustomFields { get; set; }
    }

    internal class customField
    {
        public string fieldName { get; set; }
        public string value { get; set; }
    }
}
