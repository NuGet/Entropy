using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace RestoreReplay
{
    public static class LogParser
    {
        private static readonly Regex StartRequestRegex = new Regex("^  (?<Method>GET) (?<Url>https?://.+)$");
        private static readonly Regex EndRequestRegex = new Regex("^  (?<StatusCode>OK|NotFound|InternalServerError) (?<Url>https?://.+?) (?<DurationMs>\\d+)ms$");
        private static readonly Regex OtherRequestRegex = new Regex("^\\s*https?://");

        public static RequestGraph ParseGraph(string logPath, Dictionary<string, string> stringToString)
        {
            var pendingRequests = new Dictionary<string, Queue<RequestNode>>();
            var urlToCount = new Dictionary<string, int>();
            var startedRequests = new List<RequestNode>();
            var completedRequests = new HashSet<RequestNode>(HitIndexAndUrlComparer.Instance);
            var currentConcurrency = 0;
            var maxConcurrency = 0;
            List<string> sources = null;

            Parse(
                logPath,
                stringToString,
                startRequest =>
                {
                    if (!urlToCount.TryGetValue(startRequest.Url, out var count))
                    {
                        count = 1;
                        urlToCount.Add(startRequest.Url, count);
                    }
                    else
                    {
                        count++;
                        urlToCount[startRequest.Url] = count;
                    }

                    var requestNode = new RequestNode(count - 1, startRequest, completedRequests);
                    startedRequests.Add(requestNode);

                    currentConcurrency++;
                    maxConcurrency = Math.Max(currentConcurrency, maxConcurrency);

                    if (!pendingRequests.TryGetValue(startRequest.Url, out var pendingNodes))
                    {
                        pendingNodes = new Queue<RequestNode>();
                        pendingRequests.Add(startRequest.Url, pendingNodes);
                    }

                    pendingNodes.Enqueue(requestNode);
                },
                endRequest =>
                {
                    // We assume the first response with the matching URL is associated with the first request. This is
                    // not necessarily true (A-A-B-B vs. A-B-B-A) but we must make an arbitrary decision since the logs
                    // don't have enough information to be certain.
                    var nodes = pendingRequests[endRequest.Url];
                    var requestNode = nodes.Dequeue();
                    requestNode.EndRequest = endRequest;

                    currentConcurrency--;

                    completedRequests.Add(requestNode);
                    
                    if (nodes.Count == 0)
                    {
                        pendingRequests.Remove(endRequest.Url);
                    }
                },
                parsedSources => sources = parsedSources);

            if (sources == null)
            {
                throw new InvalidDataException("No sources were found.");
            }

            return new RequestGraph(startedRequests, sources, maxConcurrency);
        }

        private static void Parse(
            string logPath,
            Dictionary<string, string> stringToString,
            Action<StartRequest> onStartRequest,
            Action<EndRequest> onEndRequest,
            Action<List<string>> onSources)
        {
            using (var fileStream = File.OpenRead(logPath))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                var inSourceList = false;
                var sources = new List<string>();
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (inSourceList)
                    {
                        if (!line.StartsWith("    "))
                        {
                            onSources(sources);
                            inSourceList = false;
                        }
                        else
                        {
                            sources.Add(DedupeString(stringToString, line.Trim()));
                        }
                    }
                    else if (TryParseStartRequest(line, stringToString, out var startRequest))
                    {
                        onStartRequest(startRequest);
                    }
                    else if (TryParseEndRequest(line, stringToString, out var endRequest))
                    {
                        onEndRequest(endRequest);
                    }
                    else if (line == "Feeds used:")
                    {
                        inSourceList = true;
                    }
                    else if (OtherRequestRegex.IsMatch(line))
                    {
                        throw new InvalidDataException("Unexpected request line: " + line);
                    }
                }

                if (inSourceList)
                {
                    onSources(sources);
                }
            }
        }
        
        private static bool TryParseStartRequest(string line, Dictionary<string, string> stringToString, out StartRequest startRequest)
        {
            var match = StartRequestRegex.Match(line);
            if (match.Success)
            {
                startRequest = new StartRequest(
                    DedupeString(stringToString, match.Groups["Method"].Value),
                    DedupeString(stringToString, match.Groups["Url"].Value.Trim()));
                return true;
            }

            startRequest = null;
            return false;
        }

        private static bool TryParseEndRequest(string line, Dictionary<string, string> stringToString, out EndRequest endRequest)
        {
            var match = EndRequestRegex.Match(line);
            if (match.Success)
            {
                endRequest = new EndRequest(
                    (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), match.Groups["StatusCode"].Value),
                    DedupeString(stringToString, match.Groups["Url"].Value.Trim()),
                    TimeSpan.FromMilliseconds(int.Parse(match.Groups["DurationMs"].Value)));
                return true;
            }

            endRequest = null;
            return false;
        }

        private static string DedupeString(Dictionary<string, string> stringToString, string input)
        {
            if (!stringToString.TryGetValue(input, out var existingUrl))
            {
                stringToString.Add(input, input);
                return input;
            }
            else
            {
                return existingUrl;
            }
        }
    }
}
