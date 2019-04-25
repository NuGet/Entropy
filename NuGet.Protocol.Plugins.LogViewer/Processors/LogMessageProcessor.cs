// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal abstract class LogMessageProcessor : ILogMessageProcessor
    {
        private readonly static Lazy<JObject> _schema = new Lazy<JObject>(GetSchema);
        private readonly string _type;

        protected LogMessageProcessor(string type)
        {
            _type = type;
        }

        public DataTable Process(IEnumerable<JObject> jObjects)
        {
            var columnNames = GetColumnNames();

            return Process(columnNames, jObjects);
        }

        protected virtual DataTable Process(IReadOnlyList<string> columnNames, IEnumerable<JObject> jObjects)
        {
            var logEntries = CreateLogEntries(jObjects);

            var dataTable = new DataTable();

            if (!logEntries.Any())
            {
                return dataTable;
            }

            dataTable.Columns.Add("Now");
            dataTable.Columns.Add("Process");

            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(Prettify(columnName));
            }

            foreach (var logEntry in logEntries)
            {
                var values = new object[] { logEntry.Now.ToString("O"), logEntry.Source }
                    .Concat(logEntry.Properties.Values)
                    .ToArray();

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        protected List<LogEntry> CreateLogEntries(IEnumerable<JObject> jObjects)
        {
            var logEntries = new List<LogEntry>();

            foreach (var jObject in jObjects)
            {
                if (string.Equals(_type, jObject.Value<string>("type"), StringComparison.Ordinal))
                {
                    logEntries.Add(LogEntry.Create(jObject));
                }
            }

            return logEntries;
        }

        protected static string Prettify(string name)
        {
            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return string.Join(" ", words.Select(word => word[0].ToString().ToUpperInvariant() + word.Substring(1)));
        }

        private IReadOnlyList<string> GetColumnNames()
        {
            var columnNames = _schema.Value[_type];

            return columnNames.Select(token => token.Value<string>()).ToArray();
        }

        private static JObject GetSchema()
        {
            Assembly assembly = typeof(MainWindow).Assembly;

            using (var stream = assembly.GetManifestResourceStream("NuGet.Protocol.Plugins.LogViewer.schema.json"))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var deserializer = new JsonSerializer();

                return (JObject)deserializer.Deserialize(jsonReader);
            }
        }

        protected sealed class LogEntry
        {
            internal string Source { get; }
            internal DateTime Now { get; }
            internal long RelativeTimeInTicks { get; }
            internal string Type { get; }
            internal Dictionary<string, string> Properties { get; }

            private LogEntry(string source, DateTime now, long relativeTimeInTicks, string type, Dictionary<string, string> properties)
            {
                Source = source;
                Now = now;
                RelativeTimeInTicks = relativeTimeInTicks;
                Type = type;
                Properties = properties;
            }

            internal static LogEntry Create(JObject jObject)
            {
                var now = DateTime.Parse(jObject.Value<string>("now"), provider: null, styles: DateTimeStyles.RoundtripKind);
                var relativeTimeInTicks = jObject.Value<long>("relative time in ticks");
                var type = jObject.Value<string>("type");
                var message = jObject.Value<JObject>("message");
                var source = jObject.Value<string>("__source__");

                var properties = message.Properties()
                    .ToDictionary(property => property.Name, property => property.Value.ToString());

                return new LogEntry(source, now, relativeTimeInTicks, type, properties);
            }
        }
    }
}