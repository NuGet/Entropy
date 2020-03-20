using System;
using System.Collections.Generic;
using System.Linq;

namespace GithubIssueTagger
{
    public class PullRequestStatistic
    {
        private Lazy<DateTimeOffset?> _firstCommentDate;
        private Lazy<IList<string>> _reviewers;

        public int PullRequestNumber { get; }
        public string Creator { get; }
        public DateTimeOffset CreatedAt { get; }
        public string State { get; }
        public DateTimeOffset? MergedDate { get; }
        public IList<ReviewStatistic> Comments { get; }

        public PullRequestStatistic(int pullNumber, string creator, DateTimeOffset createdAt, string state, DateTimeOffset? mergedDate, IList<ReviewStatistic> comments)
        {
            PullRequestNumber = pullNumber;
            Creator = creator;
            CreatedAt = createdAt;
            State = state;
            MergedDate = mergedDate;
            Comments = comments;
            _firstCommentDate = new Lazy<DateTimeOffset?>(() => GetFirstEngagementDateInternal());
            _reviewers = new Lazy<IList<string>>(() => GetReviewersInternal());
        }

        public DateTimeOffset? GetFirstEngagementDate()
        {
            return _firstCommentDate.Value;
        }

        public IList<string> GetReviewers()
        {
            return _reviewers.Value;
        }

        private DateTimeOffset? GetFirstEngagementDateInternal()
        {
            DateTimeOffset? result = null;

            if (Comments != null)
            {
                foreach (var comment in Comments)
                {
                    if (result == null || comment.CreatedAt < result)
                    {
                        result = comment.CreatedAt;
                    }
                }
            }

            return result;
        }

        private IList<string> GetReviewersInternal()
        {
            var reviewers = new HashSet<string>();
            if (Comments != null)
            {
                foreach (var comment in Comments.Where(e => e.IsReview))
                {
                    reviewers.Add(comment.Creator);
                }
            }

            return reviewers.ToList();
        }
    }

    public class ReviewStatistic
    {
        public string Creator { get; }
        public DateTimeOffset CreatedAt { get; }

        public bool IsReview { get; }

        public ReviewStatistic(string creator, DateTimeOffset createdAt, bool isReview)
        {
            Creator = creator;
            CreatedAt = createdAt;
            IsReview = isReview;
        }
    }
}
