using System;

namespace SearchScorer.Feedback
{
    [Flags]
    public enum FeedbackResultType
    {
        BothBroken = 0,
        Regressed = 1,
        Fixed = 2,
        BothFixed = 3,
    }
}
