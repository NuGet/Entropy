using System;

namespace find_buids_in_sprint.Models.ClientCiAnalysis
{
    internal record Week(string sprint, char week)
    {
        public static Week FromDate(DateTime when)
        {
            var monday = when.AddDays(-(when.DayOfWeek - DayOfWeek.Monday)).Date;
            var startOfSprint = monday.AddDays(-7 * (monday.Day / 7));
            var sprint = $"{startOfSprint.Year:d4}-{startOfSprint.Month:d2}";
            char week = (char)('A' + (when - startOfSprint).Days / 7);

            return new Week(sprint, week);
        }
    }
}
