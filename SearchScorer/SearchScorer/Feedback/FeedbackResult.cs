namespace SearchScorer.Feedback
{
    public class FeedbackResult
    {
        public FeedbackResult(
            FeedbackItem item,
            FeedbackResultType type,
            VariantResult controlResult,
            VariantResult treatmentResult)
        {
            FeedbackItem = item;
            Type = type;
            ControlResult = controlResult;
            TreatmentResult = treatmentResult;
        }

        public FeedbackItem FeedbackItem { get; }
        public FeedbackResultType Type { get; }
        public VariantResult ControlResult { get; }
        public VariantResult TreatmentResult { get; }
    }
}
