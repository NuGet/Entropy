using System.Collections.Generic;
using System.IO;

namespace RestoreTraceParser
{
    internal class UnTrackedTimeTracker
    {
        private double _untrackedTimeStampStart = -1.0;
        private List<(double timeInMilliseconds, string description)> _untrackedTimeRanges = new List<(double, string)>();

        public void OnUnTrackedTimeStart(double timeStamp)
        {
            _untrackedTimeStampStart = timeStamp;
        }

        public void OnUnTrackedTimeStop(double timeStamp, string description)
        {
            if (_untrackedTimeStampStart == -1.0)
            {
                return;
            }

            _untrackedTimeRanges.Add((timeStamp - _untrackedTimeStampStart, description));
            _untrackedTimeStampStart = -1.0;
        }

        public double TotalUntrackedTime
        {
            get
            {
                double total = 0.0;
                foreach (var range in _untrackedTimeRanges)
                {
                    total += range.timeInMilliseconds;
                }

                return total;
            }
        }

        public void WriteRanges(TextWriter writer)
        {
            writer.WriteCsvCell("Time (ms)");
            writer.WriteCsvCell("Description");
            writer.WriteLine();

            foreach (var range in _untrackedTimeRanges)
            {
                writer.WriteCsvCell(range.timeInMilliseconds.ToString("#.##"));
                writer.WriteCsvCell(range.description);
                writer.WriteLine();
            }
        }
    }
}
