using System;
using System.Globalization;

namespace LogReplay.LogParsing
{
    public static class W3CLogEntryParser
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static W3CLogEntry ParseLogEntryFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            // ignore comment rows (i.e., first row listing the column headers
            if (line.StartsWith("#"))
            {
                return null;
            }

            // columns are space-separated
            var columns = W3CParseUtils.GetLogLineRecords(line);

            var entry = new W3CLogEntry();

            // date + time
            entry.RequestDateTime = DateTimeOffset.Parse(columns[0] + " " + columns[1], null, DateTimeStyles.AssumeUniversal);

            // s-sitename
            TrySetStringProperty(value => entry.SiteName = value, columns[2]);

            // cs-method
            TrySetStringProperty(value => entry.HttpMethod = value, columns[3]);

            // cs-uri-stem
            TrySetStringProperty(value => entry.RequestPath = value, columns[4]);

            // cs-uri-query
            TrySetStringProperty(value => entry.QueryString = value, columns[5]);

            // s-port
            TrySetIntProperty(value => entry.Port = value, columns[6]);

            // cs-username
            TrySetStringProperty(value => entry.UserName = value, columns[7]);

            // c-ip
            TrySetStringProperty(value => entry.ClientIpAddress = value, columns[8]);

            // cs(User-Agent)
            TrySetStringProperty(value => entry.UserAgent = value, columns[9]);

            // cs(Cookie)
            TrySetStringProperty(value => entry.Cookie = value, columns[10]);

            // cs(Referer)
            TrySetStringProperty(value => entry.Referrer = value, columns[11]);

            // cs-host
            TrySetStringProperty(value => entry.ClientHost = value, columns[12]);

            // sc-status
            TrySetIntProperty(value => entry.StatusCode = value, columns[13]);

            // sc-substatus
            TrySetIntProperty(value => entry.SubStatusCode = value, columns[14]);

            // sc-win32-status
            TrySetStringProperty(value => entry.Win32Status = value, columns[15]);

            // sc-bytes
            TrySetLongProperty(value => entry.BytesSent = value, columns[16]);

            // cs-bytes
            TrySetLongProperty(value => entry.BytesReceived = value, columns[17]);

            // time-taken
            TrySetIntProperty(value => entry.TimeTaken = value, columns[18]);

            return entry;
        }

        private static void TrySetLongProperty(Action<long?> propertySetter, string record)
        {
            if (W3CParseUtils.RecordContainsData(record))
            {
                propertySetter(long.Parse(record));
            }
        }

        private static void TrySetIntProperty(Action<int?> propertySetter, string record)
        {
            if (W3CParseUtils.RecordContainsData(record))
            {
                propertySetter(int.Parse(record));
            }
        }

        private static void TrySetStringProperty(Action<string> propertySetter, string record)
        {
            if (W3CParseUtils.RecordContainsData(record))
            {
                propertySetter(record.Replace("\r", string.Empty).Replace("\n", string.Empty));
            }
            else
            {
                propertySetter(string.Empty);
            }
        }

        private static DateTime FromUnixTimestamp(string unixTimestamp)
        {
            // Unix timestamp is seconds past epoch
            var secondsPastEpoch = double.Parse(unixTimestamp);
            return Epoch + TimeSpan.FromSeconds(secondsPastEpoch);
        }
    }
}