// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal sealed class CommunicationLogMessageProcessor : LogMessageProcessor
    {
        private static readonly string[] _expectedStates = new[] { "Sending", "Sent", "Received", "Cancelled" };

        internal CommunicationLogMessageProcessor()
            : base("communication")
        {
        }

        protected override DataTable Process(IReadOnlyList<string> columnNames, IEnumerable<JObject> jObjects)
        {
            var logEntries = CreateLogEntries(jObjects);

            const string requestIdPropertyName = "request ID";
            const string methodPropertyName = "method";
            const string typePropertyName = "type";
            const string statePropertyName = "state";

            var requests = logEntries
                .GroupBy(entry => entry.Properties[requestIdPropertyName]);
            var states = logEntries
                .Select(entry => entry.Properties[statePropertyName])
                .Distinct();

            var dataTable = new DataTable();

            dataTable.Columns.Add("Request ID");
            dataTable.Columns.Add("Method");
            dataTable.Columns.Add("From");
            dataTable.Columns.Add("To");

            foreach (var state in _expectedStates)
            {
                dataTable.Columns.Add(state);
            }

            foreach (var request in requests)
            {
                var values = new List<object>();

                values.Add(request.Key);

                var method = request.First().Properties[methodPropertyName];

                values.Add(method);

                LogEntry fromLogEntry = request.Where(logEntry =>
                {
                    var type = logEntry.Properties[typePropertyName];
                    var state = logEntry.Properties[statePropertyName];

                    return type == "Request" && (state == "Sending" || state == "Sent");
                }).OrderBy(entry => entry.Now).ThenBy(entry => entry.RelativeTimeInTicks).FirstOrDefault();

                string from = null;

                if (fromLogEntry != null)
                {
                    from = fromLogEntry.Source;
                }

                LogEntry toLogEntry = request.Where(logEntry =>
                {
                    var type = logEntry.Properties[typePropertyName];

                    return type == "Response" && logEntry.Source != from;
                }).FirstOrDefault();

                string to = null;

                if (toLogEntry != null)
                {
                    to = toLogEntry.Source;
                }

                string sending = null;
                string sent = null;
                string received = null;

                foreach (var state in _expectedStates)
                {
                    var match = request.Where(entry => entry.Properties[statePropertyName] == state).FirstOrDefault();

                    if (match == null)
                    {
                        continue;
                    }

                    switch (state)
                    {
                        case "Sending":
                            sending = match.Now.ToString("O");
                            break;

                        case "Sent":
                            sent = match.Now.ToString("O");
                            break;

                        case "Received":
                            received = match.Now.ToString("O");
                            break;
                    }
                }

                values.Add(from);
                values.Add(to);
                values.Add(sending);
                values.Add(sent);
                values.Add(received);

                dataTable.Rows.Add(values.ToArray());
            }

            return dataTable;
        }
    }
}