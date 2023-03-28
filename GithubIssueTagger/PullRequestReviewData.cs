using System;
using System.Collections.Generic;

#nullable disable

namespace GithubIssueTagger
{
    public static partial class PullRequestUtilities
    {
        public class PullRequestReviewData
        {
            public string Name { get; }
            public int Number { get; }
            public DateTimeOffset StartedOn { get; }
            public DateTimeOffset EndedOn { get; }
            public DateTimeOffset FirstReview { get; }
            public IList<string> Reviewers { get; }

            public PullRequestReviewData(string name, int number, DateTimeOffset startedOn, DateTimeOffset endedOn, DateTimeOffset firstReview, IList<string> reviewers)
            {
                Name = name;
                Number = number;
                StartedOn = startedOn;
                EndedOn = endedOn;
                FirstReview = firstReview;
                Reviewers = reviewers;
            }
        }
    }
}
