// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal static class LogFileReader
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            DateParseHandling = DateParseHandling.None
        };

        internal static LogFileReadResult Read(FileInfo file)
        {
            var list = new List<JObject>();
            var messages = new StringBuilder();

            using (var stream = file.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    JObject jObject;

                    try
                    {
                        jObject = JsonConvert.DeserializeObject<JObject>(line, _jsonSettings);
                    }
                    catch (Exception ex)
                    {
                        messages.AppendLine($"Error deserializing `{line}` in {file.FullName}.  {ex.Message}");

                        continue;
                    }

                    list.Add(jObject);
                }
            }

            var processName = GetProcessName(list);

            foreach (var item in list)
            {
                item.Add("__source__", processName);
            }

            return new LogFileReadResult(
                list.OrderBy(item => item.Value<string>("now"))
                    .ThenBy(item => item.Value<long>("relativeTime"))
                    .ToArray(),
                messages.ToString());
        }

        private static string GetProcessName(IEnumerable<JObject> jObjects)
        {
            var process = jObjects.First(jObject => string.Equals("process", jObject.Value<string>("type"), StringComparison.Ordinal));

            var processName = process["message"].Value<string>("process name");
            var processId = process["message"].Value<int>("process ID");

            return $"{processName} ({processId})";
        }
    }
}