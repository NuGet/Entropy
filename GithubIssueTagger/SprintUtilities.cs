using System;
using System.Text.RegularExpressions;

namespace GithubIssueTagger
{
    public static class SprintUtilities
    {
        public static (DateOnly start, DateOnly end) GetSprintStartAndEnd(string sprintName)
        {
            var regex = new Regex("^(?<year>\\d{4})-(?<month>\\d{2})$");
            var result = regex.Match(sprintName);
            if (!result.Success)
            {
                throw new ArgumentException(paramName: nameof(sprintName), message: "Sprint name not in format 'yyyy-MM'");
            }

            int year = int.Parse(result.Groups["year"].ValueSpan);
            int month = int.Parse(result.Groups["month"].ValueSpan);

            return GetSprintStartAndEnd(year, month);
        }

        public static (DateOnly start, DateOnly end) GetSprintStartAndEnd(int year, int month)
        {
            AssertSupportedDate(year, month);

            DateOnly start = GetSprintStart(year, month);
            DateOnly end = month == 12
                ? GetSprintStart(year + 1, 1).AddDays(-1)
                : GetSprintStart(year, month + 1).AddDays(-1);

            return (start, end);
        }

        public static DateOnly GetSprintStart(int year, int month)
        {
            AssertSupportedDate(year, month);

            DateOnly firstOfMonth = new DateOnly(year, month, 1);
            DayOfWeek dayOfWeek = firstOfMonth.DayOfWeek;
            // 
            int firstDayOfSprint = dayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Sunday => 2,
                DayOfWeek.Saturday => 3,
                DayOfWeek.Friday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Wednesday => 6,
                DayOfWeek.Tuesday => 7,
                _ => throw new Exception("Invalid DayOfWeek '" + dayOfWeek + "'")
            };

            return new DateOnly(year, month, firstDayOfSprint);
        }

        private static void AssertSupportedDate(int year, int month)
        {
            if (month < 1 || month > 12) { throw new ArgumentOutOfRangeException(nameof(month)); }

            if ((year < 2021) || (year == 2021 && month < 1))
            {
                throw new NotSupportedException("Sprints before 2021-02 used a different cadence.");
            }
        }
    }
}
