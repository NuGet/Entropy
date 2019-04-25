// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal sealed class TaskLogMessageProcessor : LogMessageProcessor
    {
        internal TaskLogMessageProcessor()
            : base("task")
        {
        }

        protected override DataTable Process(IReadOnlyList<string> columnNames, IEnumerable<JObject> jObjects)
        {
            var logEntries = CreateLogEntries(jObjects);

            var dataTable = new DataTable();

            dataTable.Columns.Add("Now");
            dataTable.Columns.Add("Process");

            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(Prettify(columnName));
            }

            dataTable.Columns.Add("Color");

            const string requestIdPropertyName = "request ID";
            const string typePropertyName = "type";

            foreach (var logEntry in logEntries)
            {
                string color = null;

                if (logEntry.Properties[typePropertyName] == "Cancel" ||
                    logEntries.Count(entry =>
                        entry.Properties[requestIdPropertyName] == logEntry.Properties[requestIdPropertyName]
                        && entry.Properties[typePropertyName] == logEntry.Properties[typePropertyName]) == 1)
                {
                    color = "Red";
                }

                var values = new object[] { logEntry.Now.ToString("O"), logEntry.Source }
                    .Concat(logEntry.Properties.Values)
                    .Concat(new object[] { color })
                    .ToArray();

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }
    }
}