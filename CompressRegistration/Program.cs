using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CompressRegistration
{
    class Program
    {
        static async Task Download(string id, string path)
        {
            var index = new Uri(string.Format("https://api.nuget.org/v3/registration1/{0}/index.json", id.ToLowerInvariant()));

            HttpClient client = new HttpClient();

            string jsonIndex = await client.GetStringAsync(index);
            JObject objIndex = JObject.Parse(jsonIndex);

            File.WriteAllText(path + "index.json", jsonIndex);

            foreach (var item in objIndex["items"])
            {
                string pageUri = item["@id"].ToString();

                Console.WriteLine("{0}", pageUri);

                string jsonPage = await client.GetStringAsync(pageUri);
                JObject jsonObj = JObject.Parse(jsonPage);

                var lower = item["lower"];
                var upper = item["upper"];

                string filename = string.Format("{0}_{1}_{2}.json", id.ToLowerInvariant(), lower, upper);

                File.WriteAllText(path + filename, jsonPage);
            }
        }

        static void Strip(string id, string path)
        {
            var directoryInfo = new DirectoryInfo(path);

            foreach (var fileInfo in directoryInfo.EnumerateFiles(id.ToLowerInvariant() + "_*.json"))
            {
                using (FileStream stream = fileInfo.OpenRead())
                {
                    using (var textReader = new StreamReader(stream))
                    {
                        using (var jsonReader = new JsonSkipReader(new JsonTextReader(textReader)))
                        {
                            JObject obj = JObject.Load(jsonReader);
                            foreach (var item in obj["items"])
                            {
                                JToken catalogEntry = item["catalogEntry"];
                                Console.WriteLine("{0}/{1}", catalogEntry["id"], catalogEntry["version"]);
                            }
                            File.WriteAllText(fileInfo.FullName + ".txt", obj.ToString(Formatting.None));
                        }
                    }
                }
            }
        }

        static void Inline(string id, string path)
        {
            //  step (1) create a dictionary of all the JSON objects key by the resource @id they describe 

            var lookup = new Dictionary<string, JObject>();

            var directoryInfo = new DirectoryInfo(path);
            foreach (var fileInfo in directoryInfo.EnumerateFiles(id.ToLowerInvariant() + "_*.json"))
            {
                using (FileStream stream = fileInfo.OpenRead())
                {
                    using (var textReader = new StreamReader(stream))
                    {
                        using (var jsonReader =new JsonTextReader(textReader))
                        {
                            JObject objItem = JObject.Load(jsonReader);
                            string resourceId = objItem["@id"].ToString();
                            lookup[resourceId] = objItem;
                        }
                    }
                }
            }

            //  step (2) now inline these into a newly created items array and replace the current array

            string jsonIndex = File.ReadAllText(path + "index.json");
            JObject objIndex = JObject.Parse(jsonIndex);

            JArray inlinedPages = new JArray();

            foreach (var page in objIndex["items"])
            {
                string resourceId = page["@id"].ToString();
                JObject objItem = lookup[resourceId];
                JToken newPage = page.DeepClone();
                newPage["items"] = objItem["items"];
                inlinedPages.Add(newPage);
            }

            objIndex["items"] = inlinedPages;

            //  step (3) finally dump out the newly minted JSON

            File.WriteAllText(path + "inline.json", objIndex.ToString());
            File.WriteAllText(path + "inlineMinified.json", objIndex.ToString(Formatting.None));
        }

        static void Test0()
        {
            const string Id = "ravendb.client";
            const string Path = @"c:\data\scratch\";
            Download(Id, Path).Wait();
            Strip(Id, Path);
        }

        static void Test1()
        {
            const string Id = "ravendb.client";
            const string Path = @"c:\data\scratch\";
            Download(Id, Path).Wait();
            Inline(Id, Path);

            var fileInfo = new FileInfo(Path + "inline.json");

            using (var stream = fileInfo.OpenRead())
            {
                using (var textReader = new StreamReader(stream))
                {
                    using (var jsonReader = new JsonSkipReader(new JsonTextReader(textReader)))
                    {
                        JObject obj = JObject.Load(jsonReader);

                        File.WriteAllText(Path + "inlineSkipped.json", obj.ToString());
                        File.WriteAllText(Path + "inlineSkippedAndMinified.json", obj.ToString(Formatting.None));
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();
                Test1();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
