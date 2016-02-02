using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Indexing;

namespace NuGet.Test.Server
{
    public class AuxillaryIndexLoader : ILoader
    {
        public JsonReader GetReader(string name)
        {
            switch (name)
            {
                case "owners.json":
                    return Owners();
                case "curatedfeeds.json":
                    return Curatedfeeds();
                case "downloads.v1.json":
                    return Downloads();
                case "rankings.v1.json":
                    return Rankings();
            }
            return null;
        }

        JsonReader Owners()
        {
            //  array-1 of array-2 where array-2 is package-id first element and array of owners second element
            //  e.g. [["package-a",["owner-x","owner-y"]],["package-b",["owner-z"]]]
            var owners = new JArray();
            return owners.CreateReader();
        }

        JsonReader Curatedfeeds()
        {
            //  array-1 of array-2 where array-2 is feed-name first element and array of package-id second element
            //  e.g. [["feed-x",["package-a","package-b"]],["feed-y,["package-a","package-b","package-c"]]]
            var curatedfeeds = new JArray();
            return curatedfeeds.CreateReader();
        }
        JsonReader Downloads()
        {
            //  array-1 of array-2 where array-2 is package-id first element and remaining elements are 2 element array of version, downloads
            //  e.g. [["package-a",["1.0",100],["2.0",150]],["package-b",["1.0",64],["2.0",128],["3.5",256]]]
            var downloads = new JArray();
            return downloads.CreateReader();
        }
        JsonReader Rankings()
        {
            //  object with a property "Rank" where the value of every property is an array of ids in decending order by download
            //  e.g. {"Rank":["package-a","package-b","package-c"]}
            var ranking = new JObject
            {
                "Rank", new JArray(),
            };
            return ranking.CreateReader();
        }
    }
}
